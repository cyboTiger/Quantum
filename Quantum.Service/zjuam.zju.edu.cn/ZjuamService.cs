using HtmlAgilityPack;
using Microsoft.Extensions.Logging;
using Quantum.Infrastructure.Models;
using Quantum.Infrastructure.Utilities;
using System.Globalization;
using System.Net;
using System.Numerics;
using System.Text;
using System.Text.Json;

namespace zjuam.zju.edu.cn;

public class ZjuamService(ILogger<ZjuamService> logger)
    : PersistentStatefulService<ZjuamState>(
        "zjuam.json".ToDataPath(),
        state => Task.Run(() => ZjuamState.Serialize(state)),
        data => Task.Run(() => ZjuamState.Deserialize(data))
        ),
        IZjuamService
{
    private const string LoginUrl = "https://zjuam.zju.edu.cn/cas/login";
    private const string PubKeyUrl = "https://zjuam.zju.edu.cn/cas/v2/getPubKey";

    public bool IsAuthenticated => State is not null;

    public async Task<Result> ValidOrRefreshTokenAsync()
    {
        if (await IsTokenValidAsync())
        { return Result.Success(Messages.LoginSuccessful); } // 当前Cookie有效

        if (!IsAuthenticated)
        { return Result.Failure(Messages.NotLoggedIn); } // 未登录

        var username = State!.Id;
        var password = State.Password;

        await LogoutAsync();

        using var client = RequestClient.Create();

        var loginResult = await LoginAsync(username, password);
        return loginResult.IsSuccess ? Result.Success(Messages.LoginSuccessful) : Result.Failure(Messages.IncorrectUsernameOrPassword); // Cookie已过期，尝试重新登录
    }

    public string ServiceName => "浙江大学统一身份认证";
    public string LoginRoute => "/zjuam/login";
    public string LoginStatus => IsAuthenticated ? $"{State!.Id} 已登录" : "未登录";

    public async Task LogoutAsync()
    {
        OnLogout?.Invoke();
        State = null;
        await SaveStateAsync();
    }

    public event Action? OnLogout;

    public Task<Result<RequestClient>> GetAuthencatedClientAsync(RequestOptions? options = null)
    {
        if (!IsAuthenticated)
        { return Task.FromResult<Result<RequestClient>>(Result.Failure(Messages.NotLoggedIn)); }

        var requestOptions = options ?? new RequestOptions();
        requestOptions.Cookies = requestOptions.Cookies is null
            ? [State!.IPlanetDirectoryPro]
            : requestOptions.Cookies.Concat([State!.IPlanetDirectoryPro]).ToList();

        return Task.FromResult(Result.Success(RequestClient.Create(requestOptions)));
    }


    private async Task<bool> IsTokenValidAsync()
    {
        if (!IsAuthenticated)
        { return false; }

        using var client = RequestClient.Create(new RequestOptions
        {
            Cookies = [State!.IPlanetDirectoryPro]
        });

        var response = await client.GetAsync(LoginUrl);
        return response.StatusCode is HttpStatusCode.Redirect;
    }

    public async Task<Result> LoginAsync(string username, string password)
    {
        using var client = RequestClient.Create();

        // 获取登录页面HTML以解析execution值
        var execution = string.Empty;
        HttpResponseMessage response;

        for (var i = 0; i < 3; i++)
        {
            response = await client.GetAsync(LoginUrl);
            var responseBody = await response.Content.ReadAsStringAsync();

            var doc = new HtmlDocument();
            doc.LoadHtml(responseBody);
            var executionNode = doc.DocumentNode.SelectSingleNode("//input[@name='execution']");
            execution = executionNode?.GetAttributeValue("value", "");

            if (!string.IsNullOrEmpty(execution))
            { break; }
        }

        if (string.IsNullOrEmpty(execution))
        { return Result.Failure(Messages.FailedToGetExecutionValue); }

        // 获取公钥
        var pubKeyJson = JsonDocument.Parse(await client.GetStringAsync(PubKeyUrl));
        var modulus = pubKeyJson.RootElement.GetProperty("modulus").GetString();
        var exponent = pubKeyJson.RootElement.GetProperty("exponent").GetString();

        if (modulus is null)
        { return Result.Failure(Messages.FailedToGetModulus); }
        if (exponent is null)
        { return Result.Failure(Messages.FailedToGetExponent); }

        // 使用RSA公钥加密密码
        var encryptedPass = EncryptRsa(password, modulus, exponent);
        var encryptedPassStr = Convert.ToHexString(encryptedPass).ToLower().TrimStart('0');

        // 构建登录表单数据
        var formContent = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                { "username", username },
                { "password", encryptedPassStr },
                { "authcode", "" },
                { "execution", execution },
                { "_eventId", "submit" }
            });

        // 提交登录表单
        response = await client.PostAsync(LoginUrl, formContent);

        if (!response.IsSuccessStatusCode)
        {
            return Result.Failure(Messages.AccountMayBeLocked);
        }

        // 检查登录状态
        var cookieNew = client.CookieContainer.GetCookies(new Uri(LoginUrl))
            .FirstOrDefault(c => c.Name == "iPlanetDirectoryPro");

        if (cookieNew is null)
        { return Result.Failure(Messages.IncorrectUsernameOrPassword); }

        State = new ZjuamState(username, password, cookieNew);
        await SaveStateAsync();
        return Result.Success(Messages.LoginSuccessful);
    }

    private static byte[] EncryptRsa(string message, string modulus, string exponent)
    {
        // 将16进制字符串转换为BigInteger
        var n = BigInteger.Parse("00" + modulus, NumberStyles.HexNumber);
        var e = BigInteger.Parse(exponent, NumberStyles.HexNumber);

        // 将消息转换为字节数组
        var messageBytes = Encoding.UTF8.GetBytes(message);

        // 将消息字节转换为BigInteger
        var m = new BigInteger(messageBytes.Reverse().Concat(new byte[] { 0 }).ToArray());

        // 执行RSA加密: c = m^e mod n
        var c = BigInteger.ModPow(m, e, n);

        // 计算密钥长度（以字节为单位）
        var keyLength = (n.ToString("X").Length + 1) / 2;

        // 将加密后的BigInteger转换为字节数组
        var result = c.ToByteArray().Reverse().ToArray();

        // 确保结果长度正确
        if (result.Length > keyLength)
        {
            result = result.Skip(result.Length - keyLength).ToArray();
        }
        else if (result.Length < keyLength)
        {
            result = new byte[keyLength - result.Length].Concat(result).ToArray();
        }

        return result;
    }

    public bool IsInitialized { get; private set; }
    public Task InitializeTask { get; private set; } = Task.CompletedTask;
    public async Task InitializeAsync()
    {
        if (IsInitialized)
        { return; }

        InitializeTask = LoadStateAsync().ContinueWith(_ => IsInitialized = true);
        await InitializeTask;
    }
}

internal class Messages
{
    public const string UnknownError = "Unknown error occurred.";
    public const string LoginSuccessful = "Login successful.";
    public const string FailedToGetExecutionValue = "Failed to get execution value.";
    public const string FailedToGetModulus = "Failed to get modulus.";
    public const string FailedToGetExponent = "Failed to get exponent.";
    public const string IncorrectUsernameOrPassword = "Incorrect username or password.";
    public const string AccountMayBeLocked = "The account may be locked.";
    public const string NotLoggedIn = "User is not logged in.";
    public const string LogoutSuccessful = "Logout successful.";
}
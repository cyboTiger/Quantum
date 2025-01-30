using HtmlAgilityPack;
using Microsoft.Extensions.Logging;
using Quantum.Core.Interfaces;
using Quantum.Core.Models;
using Quantum.Infrastructure.Utils;
using System.Globalization;
using System.Numerics;
using System.Text;
using System.Text.Json;

namespace Quantum.Infrastructure.Services;
public class AuthenticateService(RequestClient client, ILogger<AuthenticateService> logger) : IAuthenticateService
{
    private const string BaseUrl = "http://zdbk.zju.edu.cn/jwglxt";
    private const string SsoLoginUrl = "https://zjuam.zju.edu.cn/cas/login";
    private const string SsoRedirectUrl = "/xtgl/login_ssologin.html";
    private const string GetInfoUrl = "/xsxk/zzxkghb_cxZzxkGhbIndex.html";

    public async Task<User> LoginAsync(string username, string password)
    {
        await LoginToGetCookieAsync(username, password);
        await LoginToGetSessionAsync();
        return await GetUserAsync(username) with { PasswordEncrypted = Encryption.Encrypt(password) };
    }

    public async Task<User> GetUserAsync(string username)
    {
        if (!await client.IsSessionValidAsync())
            return default!;

        var response = await client.GetAsync($"{BaseUrl}{GetInfoUrl}?gnmkdm=N253530&su={username}");
        response.EnsureSuccessStatusCode();
        var html = await response.Content.ReadAsStringAsync();

        var doc = new HtmlDocument();
        doc.LoadHtml(html);

        // 提取信息
        var name = doc.DocumentNode.SelectSingleNode("//*[@id='chi']/div/div[1]/h5/span[1]/font/b")?.InnerText ??
                   throw new HttpRequestException("Failed to get user name");
        var studentId = doc.DocumentNode.SelectSingleNode("//*[@id='chi']/div/div[1]/h5/span[1]/text()")?.InnerText ?? "";
        studentId = System.Text.RegularExpressions.Regex.Match(studentId, @"\((\d+)\)").Groups[1].Value;
        var academicYear = doc.DocumentNode.SelectSingleNode("//*[@id='xkxn']")?.InnerText ?? throw new HttpRequestException("Failed to get academic year");
        var grade = doc.DocumentNode.SelectSingleNode("//*[@id='nj']")?.GetAttributeValue("value", "") ?? throw new HttpRequestException("Failed to get grade");
        var majorCode = doc.DocumentNode.SelectSingleNode("//*[@id='zydm']")?.GetAttributeValue("value", "") ?? throw new HttpRequestException("Failed to get major code");
        var semester = doc.DocumentNode.SelectSingleNode("//*[@id='xq']")?.GetAttributeValue("value", "") ?? throw new HttpRequestException("Failed to get semester");

        logger.LogInformation($"Name: {name}\n" +
                              $"Student ID: {studentId}\n" +
                              $"Academic Year: {academicYear}\n" +
                              $"Grade: {grade}\n" +
                              $"Major Code: {majorCode}\n" +
                              $"Semester: {semester}\n");

        return new User
        {
            Id = studentId,
            Name = name,
            Grade = grade,
            Major = majorCode,
            AcademicYear = academicYear,
            Semester = semester
        };
    }

    /// <summary>
    /// 使用用户名和密码登录，获取 iPlanetDirectoryPro Cookie
    /// </summary>
    /// <param name="username">用户名</param>
    /// <param name="password">密码</param>
    /// <exception cref="HttpRequestException"></exception>
    public async Task LoginToGetCookieAsync(string username, string password)
    {
#if DEBUG
        if (username == "DEBUG" && password == "DEBUG")
            return;
#endif

        const string pubKeyUrl = "https://zjuam.zju.edu.cn/cas/v2/getPubKey";
        const string loginUrl = "https://zjuam.zju.edu.cn/cas/login";

        // 获取登录页面HTML以解析execution值
        var response = await client.GetAsync(loginUrl);
        response.EnsureSuccessStatusCode();
        var responseBody = await response.Content.ReadAsStringAsync();

        var doc = new HtmlDocument();
        doc.LoadHtml(responseBody);
        var executionNode = doc.DocumentNode.SelectSingleNode("//input[@name='execution']");
        var execution = executionNode?.GetAttributeValue("value", "")
            ?? throw new HttpRequestException("Failed to get execution value");

        // 获取公钥
        var pubKeyJson = JsonDocument.Parse(await client.GetStringAsync(pubKeyUrl));
        var modulus = pubKeyJson.RootElement.GetProperty("modulus").GetString() ?? throw new HttpRequestException("Failed to get modulus");
        var exponent = pubKeyJson.RootElement.GetProperty("exponent").GetString() ?? throw new HttpRequestException("Failed to get exponent");

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
        response = await client.PostAsync(loginUrl, formContent);

        if (!response.IsSuccessStatusCode)
        {
            throw new HttpRequestException("登录失败，可能是账号被锁定");
        }

        // 检查登录状态
        _ = client.CookieContainer.GetCookies(new Uri(loginUrl))
                   .FirstOrDefault(c => c.Name == "iPlanetDirectoryPro")
                   ?? throw new HttpRequestException("登录失败，可能是账户名或密码错误");
    }

    /// <summary>
    /// 登录到zdbk.zju.edu.cn获取JSession和route
    /// </summary>
    /// <exception cref="HttpRequestException"></exception>
    public async Task LoginToGetSessionAsync()
    {
        var ssoResponse = await client.GetAsync($"{SsoLoginUrl}?service={Uri.EscapeDataString($"{BaseUrl}{SsoRedirectUrl}")}");

        // Follow redirect to get JSESSIONID and route cookies
        if (ssoResponse.Headers.Location is not null)
        {
            var redirectResponse = await client.GetAsync(ssoResponse.Headers.Location);
            redirectResponse.EnsureSuccessStatusCode();
        }
        else
        {
            throw new HttpRequestException("SSO登录失败");
        }
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
}

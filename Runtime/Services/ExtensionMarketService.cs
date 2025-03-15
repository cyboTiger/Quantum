using Quantum.Runtime.Models;
using Quantum.Sdk.Abstractions;
using Quantum.Sdk.Services;
using Quantum.Sdk.Utilities;
using System.Net.Http.Headers;
using System.Text.Json;

namespace Quantum.Runtime.Services;

/// <summary>
/// 插件市场账户服务，负责处理插件市场的用户认证
/// </summary>
public class ExtensionMarketService : IAccountService, IInitializableService
{
    private readonly string _accountFilePath = FileUtils.GetDataFilePath("Quantum", "extension_market_account.json");

    /// <summary>
    /// 服务名称
    /// </summary>
    public string ServiceName => "插件市场";

    /// <summary>
    /// 登录路由
    /// </summary>
    public string LoginRoute => "/modules/login";

    /// <summary>
    /// 登录状态
    /// </summary>
    public string LoginStatus => IsAuthenticated ? "已登录" : "未登录";

    /// <summary>
    /// 是否已认证
    /// </summary>
    public bool IsAuthenticated => !string.IsNullOrEmpty(Data.AuthToken) && Data.CurrentUser != null;

    /// <summary>
    /// 登出事件
    /// </summary>
    public event Action? OnLogout;

    public ExtensionMarketData Data { get; private set; } = new();
    private readonly ILogger<ExtensionMarketService> _logger;

    /// <summary>
    /// 插件市场账户服务，负责处理插件市场的用户认证
    /// </summary>
    public ExtensionMarketService(ILogger<ExtensionMarketService> logger)
    {
        _logger = logger;
        InitializeTask = LoadUserDataAsync();
    }

    private async Task LoadUserDataAsync()
    {
        try
        {
            // 首先尝试从本地存储加载用户数据
            if (!File.Exists(_accountFilePath))
                return;

            var json = await File.ReadAllTextAsync(_accountFilePath);
            var userData = JsonSerializer.Deserialize<ExtensionMarketData>(Encryption.Decrypt(json));
            if (userData == null)
                return;
            Data = userData;

        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "加载用户数据失败");
        }
    }

    public async Task SaveUserDataAsync()
    {
        try
        {
            var json = JsonSerializer.Serialize(Data);

            // 确保目录存在
            var directory = Path.GetDirectoryName(_accountFilePath);
            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }

            await File.WriteAllTextAsync(_accountFilePath, Encryption.Encrypt(json));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "保存用户数据失败");
        }
    }

    /// <summary>
    /// 获取已认证的客户端
    /// </summary>
    /// <param name="options">请求选项</param>
    /// <returns>请求客户端结果</returns>
    public Task<Result<RequestClient>> GetAuthenticatedClientAsync(RequestOptions? options = null)
    {
        if (!IsAuthenticated)
        {
            return Task.FromResult<Result<RequestClient>>(Result.Failure("用户未登录"));
        }

        try
        {
            var client = RequestClient.Create(options);
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", Data.AuthToken);
            return Task.FromResult(Result.Success(client));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "创建认证客户端失败");
            return Task.FromResult<Result<RequestClient>>(Result.Failure($"创建认证客户端失败: {ex.Message}"));
        }
    }

    /// <summary>
    /// 登录方法
    /// </summary>
    /// <param name="email">用户邮箱</param>
    /// <param name="password">密码</param>
    /// <returns>登录结果</returns>
    public async Task<Result<UserDto>> LoginAsync(string email, string password)
    {
        try
        {
            var loginModel = new UserLoginDto
            {
                Email = email,
                Password = password
            };

            var client = RequestClient.Create();
            var response = await client.PostAsJsonAsync($"{Data.ExtensionMarketUrl}/Users/login", loginModel);

            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<LoginResponseDto>();
                if (result == null)
                {
                    return Result.Failure("登录响应解析失败");
                }

                Data.AuthToken = result.Token;
                Data.CurrentUser = result.User;
                await SaveUserDataAsync();

                return Result.Success(result.User!);
            }
            else
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                return Result.Failure($"登录失败: {errorContent}");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "登录失败");
            return Result.Failure($"登录出错: {ex.Message}");
        }
    }

    /// <summary>
    /// 注册方法
    /// </summary>
    /// <param name="username">用户名</param>
    /// <param name="password">密码</param>
    /// <param name="email">邮箱</param>
    /// <returns>注册结果</returns>
    public async Task<Result> RegisterAsync(string username, string password, string email)
    {
        try
        {
            var registerModel = new UserRegistrationDto
            {
                Username = username,
                Password = password,
                Email = email
            };

            var client = RequestClient.Create();
            var response = await client.PostAsJsonAsync($"{Data.ExtensionMarketUrl}/Users", registerModel);

            if (response.IsSuccessStatusCode)
            {
                return Result.Success();
            }
            else
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                return Result.Failure($"注册失败: {errorContent}");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "注册失败");
            return Result.Failure($"注册出错: {ex.Message}");
        }
    }

    /// <summary>
    /// 登出方法
    /// </summary>
    /// <returns>异步任务</returns>
    public async Task LogoutAsync()
    {
        Data.AuthToken = null;
        Data.CurrentUser = null;
        await SaveUserDataAsync();
        OnLogout?.Invoke();
    }

    public bool IsInitialized => InitializeTask.IsCompletedSuccessfully;
    public Task InitializeTask { get; }

    public class LoginResponseDto
    {
        public string? Token { get; set; }
        public UserDto? User { get; set; }
    }
}

public class ExtensionMarketData
{
    public string? AuthToken { get; set; }
    public UserDto? CurrentUser { get; set; }

    private string _extensionMarketUrl = "http://quantum.exts.koala-studio.org.cn";
    public string ExtensionMarketUrl
    {
        get => _extensionMarketUrl;
        set => _extensionMarketUrl = value.TrimEnd('/');
    }
}


using Microsoft.Extensions.Logging;
using Quantum.Infrastructure.Utilities;

namespace TemplateModule;
public class SampleService(ILogger<SampleService> logger) : ISampleService
{
    public bool IsInitialized { get; private set; }
    public Task InitializeTask { get; } = Task.CompletedTask;
    public Task InitializeAsync()
    {
        logger.LogInformation("SampleService is initialized.");
        IsInitialized = true;
        return InitializeTask;
    }

    public string SayHello() => "Hello, Quantum!";

    public string ServiceName => "示例账号服务";
    public string LoginRoute => "/home";
    public string LoginStatus { get; private set; } = "已登录";
    public bool IsAuthenticated => true;
    public Task LogoutAsync()
    {
        LoginStatus = "退不掉，气不气？";
        OnLogout?.Invoke();
        return Task.CompletedTask;
    }

    public event Action? OnLogout;
    public Task<Result<RequestClient>> GetAuthencatedClientAsync(RequestOptions? options = null) => Task.FromResult(Result.Success(RequestClient.Create()));
}

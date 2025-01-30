using Microsoft.Extensions.Logging;
using Quantum.Core.Repository;
using Quantum.Infrastructure.States;
using Quantum.Infrastructure.Utils;

namespace Quantum.Infrastructure.Services;

public class ScopeService
{
    public RequestClient Client { get; }
    public UserState UserState { get; }
    public AuthenticateService AuthenticateService { get; }

    private ScopeService(QuantumDbContext context, ILoggerFactory factory)
    {
        Client = RequestClient.LoadFromDatabase(context);
        UserState = UserState.LoadFromDatabase(context);
        AuthenticateService = new AuthenticateService(Client, new Logger<AuthenticateService>(factory));
    }

    public static async Task<ScopeService> CreateAsync(QuantumDbContext context, ILoggerFactory factory)
    {
        await context.Database.EnsureCreatedAsync();
        var service = new ScopeService(context, factory);
        await service.InitializeAsync();
        return service;
    }

    private async Task InitializeAsync()
    {
        try
        {
            if (!UserState.IsLoggedIn)
            {
                throw new Exception();
            }

            // 检查Cookie中的Session是否可用
            if (await Client.IsSessionValidAsync())
            {
                return;
            }

            // Session不可用，但Cookie可能还有效，尝试获取新Session
            try
            {
                await AuthenticateService.LoginToGetSessionAsync();
                if (await Client.IsSessionValidAsync())
                {
                    await Client.SaveToDatabaseAsync();
                    return;
                }
            }
            catch
            {
                // ignored
            }

            // Cookie已失效，如果有用户登录信息则尝试重新登录
            if (!UserState.IsLoggedIn)
                return;

            var user = UserState.CurrentUser!;
            var username = user.Id;
            var password = Encryption.Decrypt(user.PasswordEncrypted);
            var newUser = await AuthenticateService.LoginAsync(username, password);
            UserState.Login(newUser);
            await UserState.SaveToDatabaseAsync();
            await Client.SaveToDatabaseAsync();
        }
        catch
        {
            // 发生任何错误，清除所有状态
            UserState.Logout();
            await UserState.SaveToDatabaseAsync();
            await Client.SaveToDatabaseAsync();
        }
    }
}

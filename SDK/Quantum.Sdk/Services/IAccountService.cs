using Quantum.Sdk.Utilities;

namespace Quantum.Sdk.Services;

/// <summary>
/// 账户服务接口
/// </summary>
public interface IAccountService
{
    /// <summary>
    /// 服务名称
    /// </summary>
    string ServiceName { get; }

    /// <summary>
    /// 登录路由
    /// </summary>
    string LoginRoute { get; }

    /// <summary>
    /// 登录状态
    /// </summary>
    string LoginStatus { get; }

    /// <summary>
    /// 是否已认证
    /// </summary>
    bool IsAuthenticated { get; }

    /// <summary>
    /// 登出方法
    /// </summary>
    /// <returns>异步任务</returns>
    Task LogoutAsync();

    /// <summary>
    /// 登出事件
    /// </summary>
    event Action OnLogout;

    /// <summary>
    /// 获取已认证的客户端
    /// </summary>
    /// <param name="options">请求选项</param>
    /// <returns>请求客户端结果</returns>
    Task<Result<RequestClient>> GetAuthenticatedClientAsync(RequestOptions? options = null);
}

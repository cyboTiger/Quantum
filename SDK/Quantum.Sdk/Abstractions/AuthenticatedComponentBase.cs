using Microsoft.AspNetCore.Components;
using Quantum.Sdk.Services;

namespace Quantum.Sdk.Abstractions;

/// <summary>
/// 需要登录才可访问的的页面基类
/// </summary>
public abstract class AuthenticationRequiredPageBase : ComponentBase
{
    /// <summary>
    /// 导航管理器
    /// </summary>
    [Inject]
    protected NavigationManager NavigationManager { get; set; } = null!;

    /// <summary>
    /// 账户服务
    /// </summary>
    protected abstract IAccountService AccountService { get; }

    /// <summary>
    /// 渲染后执行，如果未登录则跳转到登录页面
    /// </summary>
    /// <param name="firstRender">是否首次渲染</param>
    /// <returns>异步任务</returns>
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (!AccountService.IsAuthenticated)
        {
            NavigationManager.NavigateTo(Constants.PageRoutes.AccountManagement);
        }

        await base.OnAfterRenderAsync(firstRender);
    }
}

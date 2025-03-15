using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.WebUtilities;
using Quantum.Sdk.Services;

namespace Quantum.Sdk.Abstractions;

/// <summary>
/// 登录页面基类
/// </summary>
public abstract class LoginPageBase : ComponentBase
{
    /// <summary>
    /// 导航管理器
    /// </summary>
    [Inject] protected NavigationManager NavigationManager { get; set; } = null!;

    /// <summary>
    /// 返回URL
    /// </summary>
    protected string? ReturnUrl { get; private set; }

    /// <summary>
    /// 账户服务
    /// </summary>
    protected abstract IAccountService AccountService { get; }

    /// <summary>
    /// 初始化时，获取returnUrl参数并根据登录状态跳转到目标页面
    /// </summary>
    protected override void OnInitialized()
    {
        // 获取returnUrl参数
        var uri = new Uri(NavigationManager.Uri);
        var queryParameters = QueryHelpers.ParseQuery(uri.Query);
        ReturnUrl = queryParameters.TryGetValue("returnUrl", out var returnUrlValues)
            ? Uri.UnescapeDataString(returnUrlValues.First() ?? string.Empty)
            : null;

        // 如果已经登录，直接跳转到返回地址或首页
        if (AccountService.IsAuthenticated)
        {
            NavigateToReturnUrl();
            return;
        }
    }

    /// <summary>
    /// 导航到返回URL
    /// </summary>
    protected void NavigateToReturnUrl() => NavigationManager.NavigateTo(ReturnUrl ?? "/");
}

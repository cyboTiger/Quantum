using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.WebUtilities;
using Quantum.Infrastructure.Abstractions;

namespace Quantum.Infrastructure.Models;

public abstract class LoginPageBase : ComponentBase
{
    [Inject] protected NavigationManager NavigationManager { get; set; } = default!;

    protected string? ReturnUrl { get; private set; }
    protected abstract IAccountService AccountService { get; }

    protected override void OnInitialized()
    {
        // 如果已经登录，直接跳转到返回地址或首页
        if (AccountService.IsAuthenticated)
        {
            NavigateToReturnUrl();
            return;
        }

        // 获取returnUrl参数
        var uri = new Uri(NavigationManager.Uri);
        var queryParameters = QueryHelpers.ParseQuery(uri.Query);
        ReturnUrl = queryParameters.TryGetValue("returnUrl", out var returnUrlValues)
            ? Uri.UnescapeDataString(returnUrlValues.First() ?? string.Empty)
            : null;
    }

    protected void NavigateToReturnUrl() => NavigationManager.NavigateTo(ReturnUrl ?? "/");
}

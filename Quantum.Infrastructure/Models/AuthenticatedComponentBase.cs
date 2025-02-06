using Microsoft.AspNetCore.Components;
using Quantum.Infrastructure.Abstractions;

namespace Quantum.Infrastructure.Models;

public abstract class AuthenticatedComponentBase : ComponentBase
{
    [Inject] protected NavigationManager NavigationManager { get; set; } = default!;

    protected abstract IAccountService AccountService { get; }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (!AccountService.IsAuthenticated)
        {
            var currentUri = NavigationManager.Uri;
            var loginRoute = AccountService.LoginRoute;

            // Add return url as a query parameter
            var returnUrl = Uri.EscapeDataString(currentUri);
            var redirectUrl = $"{loginRoute}?returnUrl={returnUrl}";

            NavigationManager.NavigateTo(redirectUrl);
        }

        await base.OnAfterRenderAsync(firstRender);
    }
}

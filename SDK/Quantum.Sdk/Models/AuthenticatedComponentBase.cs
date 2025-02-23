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
            NavigationManager.NavigateTo("/accounts");
        }

        await base.OnAfterRenderAsync(firstRender);
    }
}

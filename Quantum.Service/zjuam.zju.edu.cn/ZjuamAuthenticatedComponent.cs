using Microsoft.AspNetCore.Components;
using Quantum.Infrastructure.Abstractions;
using Quantum.Infrastructure.Models;

namespace zjuam.zju.edu.cn;
public abstract class ZjuamAuthenticatedComponent : AuthenticatedComponentBase
{
    [Inject] protected IZjuamService ZjuamService { get; init; } = null!;
    protected override IAccountService AccountService => ZjuamService;
}

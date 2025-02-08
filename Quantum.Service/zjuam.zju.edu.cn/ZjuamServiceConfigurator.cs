using Microsoft.Extensions.DependencyInjection;
using Quantum.Infrastructure.Abstractions;
using Quantum.Infrastructure.Extensions;

namespace zjuam.zju.edu.cn;
public class ZjuamServiceConfigurator : IModule
{
    public string ModuleId => "ZjuamService";
    public void Load(IServiceCollection services)
    {
        services.AddInitializableService<IZjuamService, ZjuamService>();
        services.AddScoped<IAccountService>(sp => sp.GetRequiredService<IZjuamService>());
    }
}

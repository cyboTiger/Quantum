using Microsoft.Extensions.DependencyInjection;
using Quantum.Infrastructure.Abstractions;
using Quantum.Infrastructure.Extensions;
using zdbk.zju.edu.cn;

namespace zjuam.zju.edu.cn;
public class ZdbkServiceConfigurator : IModule
{
    public string ModuleId => "ZdbkService";
    public void Load(IServiceCollection services)
    {
        services.AddInitializableService<IZdbkService, ZdbkService>();
    }
}

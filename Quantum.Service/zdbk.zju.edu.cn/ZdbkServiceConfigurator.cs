using Microsoft.Extensions.DependencyInjection;
using Quantum.Infrastructure.Abstractions;
using Quantum.Infrastructure.Extensions;
using zdbk.zju.edu.cn;

namespace zjuam.zju.edu.cn;
public class ZdbkServiceConfigurator : IServiceConfigurator
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddInitializableService<IZdbkService, ZdbkService>();
    }
}

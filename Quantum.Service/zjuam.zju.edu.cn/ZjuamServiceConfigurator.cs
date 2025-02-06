using Microsoft.Extensions.DependencyInjection;
using Quantum.Infrastructure.Abstractions;
using Quantum.Infrastructure.Extensions;

namespace zjuam.zju.edu.cn;
public class ZjuamServiceConfigurator : IServiceConfigurator
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddInitializableService<IZjuamService, ZjuamService>();
        services.AddScoped<IAccountService>(sp => sp.GetRequiredService<IZjuamService>());
    }
}

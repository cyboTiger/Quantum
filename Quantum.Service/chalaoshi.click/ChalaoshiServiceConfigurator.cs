using chalaoshi.click;
using Microsoft.Extensions.DependencyInjection;
using Quantum.Infrastructure.Abstractions;
using Quantum.Infrastructure.Extensions;

namespace zjuam.zju.edu.cn;
public class ChalaoshiServiceConfigurator : IServiceConfigurator
{
    public void ConfigureServices(IServiceCollection services) => services.AddInitializableService<IChalaoshiService, ChalaoshiService>();
}

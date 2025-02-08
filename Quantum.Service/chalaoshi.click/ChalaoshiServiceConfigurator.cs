using chalaoshi.click;
using Microsoft.Extensions.DependencyInjection;
using Quantum.Infrastructure.Abstractions;
using Quantum.Infrastructure.Extensions;

namespace zjuam.zju.edu.cn;
public class ChalaoshiServiceConfigurator : IModule
{
    public string ModuleId => "ChalaoshiService";
    public void Load(IServiceCollection services) => services.AddInitializableService<IChalaoshiService, ChalaoshiService>();
}

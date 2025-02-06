using Quantum.Infrastructure.Abstractions;
using Quantum.Infrastructure.Utilities;

namespace Quantum.Infrastructure.Extensions;

public static class ServiceCollectionExtension
{
    public static IServiceCollection AddInitializableService<TService, TImplement>(this IServiceCollection services)
        where TService : class, IInitializableService
        where TImplement : class, TService
    {
        services.AddScoped<TService, TImplement>(sp =>
        {
            var instance = Assertion.NotNull(ActivatorUtilities.GetServiceOrCreateInstance<TImplement>(sp));
            instance.InitializeAsync();
            return instance;
        });
        services.AddScoped<IInitializableService>(sp => sp.GetRequiredService<TService>());
        return services;
    }

    /// <summary>
    /// 添加所有模块的服务
    /// </summary>
    public static IServiceCollection AddModule<T>(this IServiceCollection services)
        where T : IModule
    {
        var module = Activator.CreateInstance<T>();
        services.AddSingleton<IModule>(module);
        module.ConfigureServices(services);
        return services;
    }

    public static IServiceCollection ConfigureService<T>(this IServiceCollection services)
        where T : IServiceConfigurator
    {
        Activator.CreateInstance<T>().ConfigureServices(services);
        return services;
    }
}

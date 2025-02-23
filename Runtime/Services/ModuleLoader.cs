using Quantum.Infrastructure.Abstractions;
using Quantum.Infrastructure.Models;
using System.Reflection;

namespace Quantum.Shell.Services;

internal class ModuleLoader(ILogger<ModuleLoader> logger, IServiceCollection services, InjectedCodeManager codeManager)
{
    private readonly List<IModule> _loadedModules = [];
    private readonly List<Assembly> _loadedAssemblies = [];
    public IServiceProvider ServiceProvider { get; set; } = null!;
    public IReadOnlyList<Assembly> LoadedAssemblies => _loadedAssemblies.AsReadOnly();


    public async Task LoadModulesAsync()
    {
        // 扫描模块目录
        var modulesPath = Path.Combine(AppContext.BaseDirectory, "Modules");
        if (!Directory.Exists(modulesPath))
        {
            logger.LogWarning("Modules directory not found at {Path}", modulesPath);
            return;
        }

        // 加载模块程序集
        foreach (var moduleDir in Directory.GetDirectories(modulesPath))
        {
            var moduleName = Path.GetFileName(moduleDir);
            var dllPath = Path.Combine(moduleDir, $"{moduleName}.dll");

            if (!File.Exists(dllPath))
            {
                logger.LogWarning("Module DLL not found at {Path}", dllPath);
                continue;
            }

            var assembly = Assembly.LoadFrom(dllPath);
            _loadedAssemblies.Add(assembly);
        }

        _loadedAssemblies.ForEach(RegisterModule);

        // 调用所有模块的 OnAllLoaded 方法
        var initTasks = _loadedModules.Select(module =>
            module.OnAllLoadedAsync(_loadedModules)).ToList();

        await Task.WhenAll(initTasks);

        logger.LogInformation("All modules loaded successfully. Total modules: {Count}", _loadedModules.Count);
    }

    public void LoadModule(Assembly assembly)
    {
        _loadedAssemblies.Add(assembly);
        RegisterModule(assembly);
    }

    private void RegisterModule(Assembly assembly)
    {
        var moduleTypes = assembly.GetTypes()
            .Where(t => typeof(IModule).IsAssignableFrom(t) && t is { IsAbstract: false, IsInterface: false })
            .ToList();

        switch (moduleTypes.Count)
        {
            case 0:
                logger.LogWarning("No module implementation found in assembly {Assembly}", assembly.FullName);
                return;
            case > 1:
                throw new InvalidOperationException(
                    $"Multiple module implementations found in assembly {assembly.FullName}. Only one implementation of IModule is allowed per assembly.");
        }

        var moduleType = moduleTypes[0];
        // 创建模块实例并添加到已加载模块列表
        var module = (IModule)ActivatorUtilities.CreateInstance(ServiceProvider, moduleType);
        _loadedModules.Add(module);

        // 注册模块类型本身
        services.AddSingleton(moduleType, module);

        // 注册为 IModule
        services.AddSingleton(module);

        // 如果实现了 IUiModule，额外注册
        if (typeof(IUiModule).IsAssignableFrom(moduleType))
        {
            var moduleName = assembly.GetName().Name;
            var styleFile = $"_content/{moduleName}/{moduleName}.styles.css";
            var stylePath = Path.Combine(AppContext.BaseDirectory, "wwwroot", styleFile);

            if (File.Exists(stylePath))
            {
                codeManager.AddToHead($"<link rel='stylesheet' href='{styleFile}' />");
                logger.LogInformation("Injected stylesheet for module {ModuleId}: {StyleFile}", module.ModuleId, styleFile);
            }
            services.AddSingleton((IUiModule)module);
        }

        logger.LogInformation("Registered module {ModuleId} of type {ModuleType}",
            module.ModuleId, moduleType.FullName);

    }
}

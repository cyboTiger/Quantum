using ElectronNET.API;
using ElectronNET.API.Entities;
using Quantum.Runtime.Services;
using Quantum.Sdk;
using Quantum.Sdk.Extensions;
using Quantum.Sdk.Services;
using Serilog;
using Serilog.Events;

// 处理待安装的模块
HandlePendingModules();

// 处理待卸载的模块
HandleModulesToUninstall();

var builder = WebApplication.CreateBuilder(args);

builder.WebHost.UseElectron(args);

#if RELEASE
// Configure Serilog
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Debug()
    .MinimumLevel.Override("Microsoft", LogEventLevel.Information)
    .Enrich.FromLogContext()
    .WriteTo.File(
        Path.Combine("logs", $"log_{DateTime.Now:yyyy-MM-dd.HH.mm.ss}.txt"),
        outputTemplate: "[{Timestamp:yyyy-MM-dd HH:mm:ss.fff}] [{Level:u3}] [{SourceContext}] {Message:lj}{NewLine}{Exception}",
        rollingInterval: RollingInterval.Day,
        fileSizeLimitBytes: 10 * 1024 * 1024)
    .CreateLogger();
#else
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Debug()
    .MinimumLevel.Override("Microsoft", LogEventLevel.Information)
    .Enrich.FromLogContext()
    .WriteTo.Console(outputTemplate: "[{Level:u3}] [{SourceContext}] {Message:lj}{NewLine}{Exception}")
    .CreateLogger();
#endif

builder.Host.UseSerilog();

var preloadServices = new ServiceCollection();
preloadServices.AddLogging(logBuilder => logBuilder.AddSerilog())
    .AddSingleton<InjectedCodeManager>()
    .AddSingleton(sp =>
    {
        var logger = sp.GetRequiredService<ILogger<ModuleManager>>();
        var moduleManager = new ModuleManager(logger) { Activator = sp, HostServices = builder.Services };
        return moduleManager;
    })
    .AddSingleton<IQuantum>(sp =>
    {
        var moduleManager = sp.GetRequiredService<ModuleManager>();
        var codeManager = sp.GetRequiredService<InjectedCodeManager>();
        var quantum = new Quantum.Runtime.Services.Quantum { HostServices = builder.Services, ModuleManager = moduleManager, InjectedCodeManager = codeManager };
        return quantum;
    });

#pragma warning disable ASP0000 // Do not call 'IServiceCollection.BuildServiceProvider' in 'ConfigureServices'
var preloadProvider = preloadServices.BuildServiceProvider();
#pragma warning restore ASP0000 // Do not call 'IServiceCollection.BuildServiceProvider' in 'ConfigureServices'

var quantum = (Quantum.Runtime.Services.Quantum)preloadProvider.GetRequiredService<IQuantum>();
var injectedCodeManager = preloadProvider.GetRequiredService<InjectedCodeManager>();
var moduleManager = preloadProvider.GetRequiredService<ModuleManager>();

#region MODULE_DEBUG
#if DEBUG
// 在这里手动加载模块，方便调试
moduleManager.LoadModule(typeof(TemplateUiModule.TemplateUiModule).Assembly);
moduleManager.LoadModule(typeof(SearchUiModule.SearchUiModule).Assembly);
moduleManager.LoadModule(typeof(TemplateModule.TemplateModule).Assembly);
moduleManager.LoadModule(typeof(SearchModule.SearchModule).Assembly);
#endif
#endregion

// 首先在 Program.cs 或 Startup.cs 中注册 HttpClient
builder.Services.AddHttpClient();

await moduleManager.RegisterModulesAsync();

// Add services to the container.
builder.Services
    .AddEagerInitializeService<ExtensionMarketService, ExtensionMarketService>()
    .AddSingleton<IAccountService>(sp => sp.GetRequiredService<ExtensionMarketService>())
    .AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
            .AllowAnyMethod()
            .AllowAnyHeader();
    });
});

builder.Services.AddAntDesign()
    .AddSingleton<IQuantum>(quantum)
    .AddSingleton(injectedCodeManager);

WebApplication app;
try
{
    app = builder.Build();
}
catch (Exception ex)
{
    #region ErrorResolver
    // Log the exception
    Log.Error(ex, "Application startup failed");

    // Create a minimal application to show an error page
    var errorBuilder = WebApplication.CreateBuilder(args);
    errorBuilder.WebHost.UseElectron(args);

    // Add minimal services required for Blazor error page
    errorBuilder.Services.AddRazorComponents()
        .AddInteractiveServerComponents();

    // Add logging
    errorBuilder.Services.AddLogging(logging => logging.AddSerilog());
    errorBuilder.Services.AddAntDesign();

    app = errorBuilder.Build();

    // Configure minimal error pipeline
    app.UseStaticFiles();
    app.UseAntiforgery();

    // Map Blazor components with ErrorApp as the root component
    app.MapRazorComponents<Quantum.Runtime.ErrorApp>()
        .AddInteractiveServerRenderMode();

    // Pass the error message as a parameter to the root component
    app.Use(async (context, next) =>
    {
        if (context.Request.Path == "/")
        {
            context.Request.QueryString = context.Request.QueryString.Add("errorMessage", ex.Message);
        }
        await next();
    });

    // Skip the rest of the normal startup configuration
    goto StartApp;
    #endregion
}

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
}

app.MapStaticAssets();
app.UseStaticFiles(new StaticFileOptions
{
    OnPrepareResponse = ctx =>
    {
        // 禁用所有静态文件的缓存
        ctx.Context.Response.Headers.Append("Cache-Control", "no-cache, no-store");
        ctx.Context.Response.Headers.Append("Pragma", "no-cache");
        ctx.Context.Response.Headers.Append("Expires", "-1");
    }
})
    .UseCors("AllowAll")
    .UseAntiforgery();

app.MapRazorComponents<Quantum.Runtime.App>()
    .AddInteractiveServerRenderMode()
    .AddAdditionalAssemblies([.. moduleManager.LoadedAssemblies]);

StartApp:
if (HybridSupport.IsElectronActive)
{
    await app.StartAsync();

    var window = await Electron.WindowManager.CreateWindowAsync(new BrowserWindowOptions
    {
        Frame = false,
        Show = false,
        MinWidth = 1024,
        MinHeight = 768,
        WebPreferences = new WebPreferences
        {
            NodeIntegration = false,
            ContextIsolation = true,
            Sandbox = false,
#if DEBUG
            DevTools = true
#endif
        }
    });
    quantum.Window = window;

    window.RemoveMenu();

#if DEBUG
    window.WebContents.OpenDevTools();
#endif

    window.OnReadyToShow += () =>
    {
        window.Show();
    };

    window.OnClosed += () => app.StopAsync();

    await Task.Run(() => app.WaitForShutdown());
}
else
{
    await app.RunAsync();
}

return;

static void HandlePendingModules()
{
    var pendingPath = Path.Combine(AppContext.BaseDirectory, "PendingModule");
    if (!Directory.Exists(pendingPath))
        return;

    foreach (var moduleFolder in Directory.GetDirectories(pendingPath))
    {
        try
        {
            var moduleName = Path.GetFileName(moduleFolder);
            var wwwrootSource = Path.Combine(moduleFolder, "wwwroot");
            var moduleTarget = Path.Combine(AppContext.BaseDirectory, "Modules", moduleName);
            var wwwrootTarget = Path.Combine(AppContext.BaseDirectory, "wwwroot");

            // 处理wwwroot中的静态资源
            if (Directory.Exists(wwwrootSource))
            {
                var moduleContentTarget = Path.Combine(wwwrootTarget, "_content", moduleName);
                var contentSource = Path.Combine(wwwrootSource, "_content");

                // 如果存在 _content 目录，直接合并到运行目录的 wwwroot/_content
                if (Directory.Exists(contentSource))
                {
                    CopyDirectory(contentSource, Path.Combine(wwwrootTarget, "_content"), true);
                    Directory.Delete(contentSource, true);
                }

                // 将其他文件移动到 _content/{moduleName} 下
                Directory.CreateDirectory(moduleContentTarget);
                foreach (var item in Directory.GetFileSystemEntries(wwwrootSource))
                {
                    var itemName = Path.GetFileName(item);
                    if (itemName == "_content")
                        continue;

                    var targetPath = Path.Combine(moduleContentTarget, itemName);
                    if (File.Exists(item))
                    {
                        File.Copy(item, targetPath, true);
                        File.Delete(item);
                    }
                    else if (Directory.Exists(item))
                    {
                        CopyDirectory(item, targetPath, true);
                        Directory.Delete(item, true);
                    }
                }

                Directory.Delete(wwwrootSource, true);
            }

            // 将剩余内容复制到Modules文件夹
            Directory.CreateDirectory(Path.GetDirectoryName(moduleTarget)
                ?? throw new InvalidOperationException($"无法获取目录名称: {moduleTarget}"));
            if (Directory.Exists(moduleTarget))
                Directory.Delete(moduleTarget, true);

            CopyDirectory(moduleFolder, moduleTarget, true);
            Directory.Delete(moduleFolder, true);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"处理待安装模块失败: {ex}");
        }
    }

    try
    {
        if (Directory.Exists(pendingPath))
            Directory.Delete(pendingPath, true);
    }
    catch (Exception ex)
    {
        Console.WriteLine($"清理PendingModule目录失败: {ex}");
    }
}

static void HandleModulesToUninstall()
{
    var modulesPath = Path.Combine(AppContext.BaseDirectory, "Modules");
    if (!Directory.Exists(modulesPath))
        return;

    foreach (var moduleFolder in Directory.GetDirectories(modulesPath))
    {
        try
        {
            var uninstallMark = Path.Combine(moduleFolder, "TobeUninstalled.Quantum.MarkTag");
            if (!File.Exists(uninstallMark))
                continue;

            var moduleName = Path.GetFileName(moduleFolder);
            var moduleContentPath = Path.Combine(AppContext.BaseDirectory, "wwwroot", "_content", moduleName);

            // 删除模块的静态资源
            if (Directory.Exists(moduleContentPath))
                Directory.Delete(moduleContentPath, true);

            // 删除模块目录
            Directory.Delete(moduleFolder, true);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"处理待卸载模块失败: {ex}");
        }
    }
}

static void CopyDirectory(string sourceDir, string targetDir, bool overwrite)
{
    Directory.CreateDirectory(targetDir);

    foreach (var file in Directory.GetFiles(sourceDir))
    {
        var fileName = Path.GetFileName(file);
        var targetPath = Path.Combine(targetDir, fileName);
        File.Copy(file, targetPath, overwrite);
    }

    foreach (var directory in Directory.GetDirectories(sourceDir))
    {
        var dirName = Path.GetFileName(directory);
        var targetPath = Path.Combine(targetDir, dirName);
        CopyDirectory(directory, targetPath, overwrite);
    }
}

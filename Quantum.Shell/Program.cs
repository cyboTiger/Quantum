using ElectronNET.API;
using ElectronNET.API.Entities;
using Quantum.Infrastructure.Models;
using Quantum.Shell.Services;

#if RELEASE
using Serilog;
using Serilog.Events;
#endif

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
        outputTemplate: "[{Timestamp:yyyy-MM-dd HH:mm:ss.fff}] [{Level:u3}] [{SourceContext}] [{FilePath}:{LineNumber}] {Message:lj}{NewLine}{Exception}",
        rollingInterval: RollingInterval.Day,
        fileSizeLimitBytes: 10 * 1024 * 1024)
    .CreateLogger();

builder.Host.UseSerilog();
#endif

// Add services to the container.
builder.Services.AddRazorComponents()
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

var codeManager = new InjectedCodeManager();

builder.Services.AddAntDesign()
                .AddSingleton(codeManager)
                .AddSingleton<ModuleLoader>()
                .AddSingleton(builder.Services);

var provider = builder.Services.BuildServiceProvider();
var loader = provider.GetRequiredService<ModuleLoader>();
loader.ServiceProvider = provider;
await loader.LoadModulesAsync();

var app = builder.Build();

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

app.MapRazorComponents<Quantum.Shell.App>()
    .AddInteractiveServerRenderMode()
    .AddAdditionalAssemblies([.. loader.LoadedAssemblies]);

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

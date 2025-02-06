using ElectronNET.API;
using ElectronNET.API.Entities;
using Quantum.Infrastructure.Abstractions;
using Quantum.Infrastructure.Extensions;
using zjuam.zju.edu.cn;

#if RELEASE
using Serilog;
using Serilog.Events;
#endif

var builder = WebApplication.CreateBuilder(args);

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

builder.WebHost.UseElectron(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// Add CORS policy
builder.Services
    .AddCors(options =>
    {
        options.AddPolicy("AllowAll", policy =>
        {
            policy.AllowAnyOrigin()
                .AllowAnyMethod()
                .AllowAnyHeader();
        });
    })
    .AddAntDesign()
    .ConfigureService<ZjuamServiceConfigurator>()
    .ConfigureService<ZdbkServiceConfigurator>()
    .ConfigureService<ChalaoshiServiceConfigurator>()
    .AddModule<Quantum.UI.CourseSelectionAssistant.CsaModule>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
}

app.MapStaticAssets();
app.UseCors("AllowAll")
   .UseAntiforgery();

var modules = app.Services.GetServices<IModule>();

app.MapRazorComponents<Quantum.UI.Shell.Layout.App>()
    .AddInteractiveServerRenderMode()
    .AddAdditionalAssemblies(modules.GetAssemblies().ToArray());

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
    // 等待一段时间确保资源加载完成
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

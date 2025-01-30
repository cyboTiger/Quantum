using ElectronNET.API;
using ElectronNET.API.Entities;
using Quantum.Core.Interfaces;
using Quantum.Core.Repository;
using Quantum.Infrastructure.Services;
using Quantum.Infrastructure.States;
using Serilog;
using Serilog.Events;

var builder = WebApplication.CreateBuilder(args);

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
builder.WebHost.UseElectron(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// Add CORS policy
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

// Register services
builder.Services.AddAntDesign();

builder.Services.AddScoped<QuantumDbContext>();
builder.Services.AddScoped(sp =>
{
    var context = sp.GetRequiredService<QuantumDbContext>();
    var factory = sp.GetRequiredService<ILoggerFactory>();
    var task = Task.Run(async () => await ScopeService.CreateAsync(context, factory));
    return task.GetAwaiter().GetResult();
});

builder.Services.AddScoped(sp => sp.GetRequiredService<ScopeService>().Client);
builder.Services.AddScoped(sp => sp.GetRequiredService<ScopeService>().UserState);
builder.Services.AddScoped<IAuthenticateService>(sp => sp.GetRequiredService<ScopeService>().AuthenticateService);

builder.Services.AddScoped<ICourseScrapingService, CourseScrapingService>();
builder.Services.AddScoped<IGraduationRequirementService, GraduationRequirementService>();

builder.Services.AddScoped<WishListState>();
builder.Services.AddScoped<ITeacherScrapingService, TeacherScrapingService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseCors("AllowAll");

app.UseStaticFiles();
app.UseAntiforgery();

app.MapRazorComponents<Quantum.UI.Components.App>()
    .AddInteractiveServerRenderMode();

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
            NodeIntegration = true,
            ContextIsolation = false
        }
    });

    window.RemoveMenu();
    window.OnReadyToShow += () =>
    {
        window.Show();
        window.Maximize();
    };
    window.OnClosed += () => app.StopAsync();

    await Task.Run(() => app.WaitForShutdown());
}
else
{
    app.Run();
}

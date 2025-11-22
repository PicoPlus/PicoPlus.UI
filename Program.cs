using Blazored.LocalStorage;
using Blazored.SessionStorage;
using DotNetEnv;
using Microsoft.AspNetCore.Localization;
using PicoPlus.Components;
using PicoPlus.Infrastructure.Authorization;
using PicoPlus.Infrastructure.Http;
using PicoPlus.Infrastructure.Plugins;
using PicoPlus.Infrastructure.Services;
using PicoPlus.Infrastructure.State;
using PicoPlus.Services.Admin;
using PicoPlus.Services.Auth;
using PicoPlus.State.Admin;
using System.Globalization;

var builder = WebApplication.CreateBuilder(args);

// ==========================================
// 1. Configuration & Environment
// ==========================================
var envPath = Path.Combine(Directory.GetCurrentDirectory(), ".env");
if (File.Exists(envPath))
{
    Env.Load(envPath);
}

builder.Configuration
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
    .AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.json", optional: true, reloadOnChange: true)
    .AddEnvironmentVariables()
    .AddUserSecrets<Program>(optional: true);

// ==========================================
// 2. Logging & Kestrel
// ==========================================
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDebug();

if (builder.Environment.IsProduction())
{
    builder.WebHost.ConfigureKestrel(options => options.ListenAnyIP(5000)); // HTTP only for Docker/Liara
    builder.Logging.AddFilter("Microsoft", LogLevel.Warning);
    builder.Logging.AddFilter("System", LogLevel.Warning);
    builder.Logging.SetMinimumLevel(LogLevel.Information);
}
else
{
    builder.Logging.SetMinimumLevel(LogLevel.Debug);
}

// ==========================================
// 3. Core Services (Localization, Cache, Razor)
// ==========================================
builder.Services.AddLocalization(options => options.ResourcesPath = "Resources");
var supportedCultures = new[] { new CultureInfo("fa-IR"), new CultureInfo("en-US") };
builder.Services.Configure<RequestLocalizationOptions>(options =>
{
    options.DefaultRequestCulture = new RequestCulture("fa-IR");
    options.SupportedCultures = supportedCultures;
    options.SupportedUICultures = supportedCultures;
});

builder.Services.AddResponseCompression(options => options.EnableForHttps = true);
builder.Services.AddMemoryCache();
builder.Services.AddRazorPages();
builder.Services.AddRazorComponents().AddInteractiveServerComponents();

// ==========================================
// 4. Storage & State Management
// ==========================================
builder.Services.AddBlazoredSessionStorage();
builder.Services.AddBlazoredLocalStorage();
builder.Services.AddScoped<ISessionStorageService, SessionStorageServiceWrapper>();
builder.Services.AddScoped<ILocalStorageService, LocalStorageServiceWrapper>();
builder.Services.AddSingleton<ToastService>();
builder.Services.AddScoped<IDialogService, DialogServiceWrapper>();
builder.Services.AddScoped<INavigationService, NavigationService>();
builder.Services.AddSingleton<AuthenticationStateService>();

// ==========================================
// 5. Auth & Admin Services
// ==========================================
builder.Services.AddScoped<AuthService>();
builder.Services.AddScoped<AdminAuthorizationHandler>();
builder.Services.AddScoped<AdminStateService>();
builder.Services.AddScoped<AdminOwnerService>();
builder.Services.AddScoped<DashboardService>();
builder.Services.AddScoped<KanbanService>();

// ==========================================
// 6. CRM & External APIs (HttpClients)
// ==========================================

// HubSpot Client
builder.Services.AddHttpClient("HubSpot", (sp, client) =>
{
    var config = sp.GetRequiredService<IConfiguration>();
    client.BaseAddress = new Uri("https://api.hubapi.com");
    client.Timeout = TimeSpan.FromSeconds(30);

    var token = Environment.GetEnvironmentVariable("HUBSPOT_TOKEN") ?? config["HubSpot:Token"];
    if (!string.IsNullOrEmpty(token))
    {
        client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
    }
    client.DefaultRequestHeaders.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));
});

// Zibal Client
builder.Services.AddHttpClient<PicoPlus.Services.Identity.Zibal>(client =>
{
    client.BaseAddress = new Uri("https://api.zibal.ir");
    client.Timeout = TimeSpan.FromSeconds(30);
});

// CRM Objects
builder.Services.AddScoped<PicoPlus.Services.CRM.Objects.Contact>();
builder.Services.AddScoped<PicoPlus.Services.CRM.Objects.Deal>();
builder.Services.AddScoped<PicoPlus.Services.CRM.Objects.Company>();
builder.Services.AddScoped<PicoPlus.Services.CRM.Objects.Ticket>();
builder.Services.AddScoped<PicoPlus.Services.CRM.ContactUpdateService>();
builder.Services.AddScoped<PicoPlus.Services.CRM.Pipelines>();
builder.Services.AddScoped<PicoPlus.Services.CRM.Associate>();
builder.Services.AddScoped<PicoPlus.Services.CRM.Owners>();

// CRM Commerce HttpClients
builder.Services.AddHttpClient<PicoPlus.Services.CRM.Commerce.Product>(client =>
{
    client.BaseAddress = new Uri("https://api.hubapi.com");
    client.Timeout = TimeSpan.FromSeconds(30);
});
builder.Services.AddHttpClient<PicoPlus.Services.CRM.Commerce.LineItem>(client =>
{
    client.BaseAddress = new Uri("https://api.hubapi.com");
    client.Timeout = TimeSpan.FromSeconds(30);
});

// ==========================================
// 7. SMS Services (Cleaned - No Shecan)
// ==========================================
builder.Services.AddHttpClient<PicoPlus.Services.SMS.SmsIr>(client =>
{
    client.BaseAddress = new Uri("https://api.sms.ir");
    client.Timeout = TimeSpan.FromSeconds(30);
});
// Note: ConfigurePrimaryHttpMessageHandler removed here to bypass Shecan

builder.Services.AddScoped<PicoPlus.Services.SMS.SMS.Send>();
builder.Services.AddScoped<PicoPlus.Services.SMS.SmsIrService>();
builder.Services.AddScoped<PicoPlus.Services.SMS.FarazSmsService>();
builder.Services.AddScoped<PicoPlus.Services.SMS.SmsServiceFactory>();
builder.Services.AddScoped<PicoPlus.Services.SMS.ISmsService, PicoPlus.Services.SMS.SmsService>();
builder.Services.AddSingleton<OtpService>();

// ==========================================
// 8. ViewModels & UI Services
// ==========================================
#region ViewModels
builder.Services.AddScoped<PicoPlus.ViewModels.Auth.LoginViewModel>();
builder.Services.AddScoped<PicoPlus.ViewModels.Auth.AdminLoginViewModel>();
builder.Services.AddScoped<PicoPlus.ViewModels.Auth.RegisterViewModel>();
builder.Services.AddScoped<PicoPlus.ViewModels.User.UserHomeViewModel>();
builder.Services.AddScoped<PicoPlus.ViewModels.Deal.DealCreateViewModel>();
builder.Services.AddScoped<PicoPlus.ViewModels.Deal.DealCreateDialogViewModel>();
#endregion

builder.Services.AddSingleton<PicoPlus.Services.UserPanel.IPersianDateService, PicoPlus.Services.UserPanel.PersianDateService>();
builder.Services.AddScoped<PicoPlus.Services.UserPanel.IUserPanelService, PicoPlus.Services.UserPanel.UserPanelService>();
builder.Services.AddScoped<PicoPlus.Views.Deal.Create>();
builder.Services.AddScoped<PicoPlus.Services.Imaging.ImageProcessingService>();

// ==========================================
// 9. Plugin System Initialization
// ==========================================
// Warning: Building a temp provider can be risky, ensure plugins don't depend on singletons created here later.
var tempServiceProvider = builder.Services.BuildServiceProvider();
var pluginManager = new PluginManager(
    builder.Services,
    tempServiceProvider,
    builder.Configuration,
    builder.Environment,
    tempServiceProvider.GetRequiredService<ILoggerFactory>()
);
builder.Services.AddSingleton(pluginManager);

var app = builder.Build();

// ==========================================
// 10. Post-Build Setup (Plugins & Pipeline)
// ==========================================
var logger = app.Services.GetRequiredService<ILogger<Program>>();

logger.LogInformation("🚀 Starting Plugin Discovery...");
try
{
    await pluginManager.DiscoverAndLoadPluginsAsync();
    logger.LogInformation("✅ Plugins loaded successfully.");
}
catch (Exception ex)
{
    logger.LogError(ex, "❌ Failed to initialize plugin system.");
}

// Debug Info Logging
if (app.Environment.IsDevelopment())
{
    logger.LogInformation("--- Debug Configuration ---");
    logger.LogInformation("Env File Loaded: {HasEnv}", File.Exists(envPath));
    logger.LogInformation("HubSpot Token Set: {HasHubSpot}", !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("HUBSPOT_TOKEN") ?? app.Configuration["HubSpot:Token"]));
    logger.LogInformation("---------------------------");
}

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

var locOptions = app.Services.GetRequiredService<Microsoft.Extensions.Options.IOptions<RequestLocalizationOptions>>().Value;
app.UseRequestLocalization(locOptions);
app.UseResponseCompression();

// Static Files Caching
var cacheMaxAge = app.Environment.IsProduction() ? TimeSpan.FromDays(30) : TimeSpan.FromSeconds(0);
app.UseStaticFiles(new StaticFileOptions
{
    OnPrepareResponse = ctx =>
    {
        ctx.Context.Response.Headers.Append("Cache-Control", $"public,max-age={cacheMaxAge.TotalSeconds}");
    }
});

// Culture Switching Helper
app.MapGet("/set-culture/{culture}", (string culture, string? redirectUri, HttpContext httpContext) =>
{
    if (!string.IsNullOrWhiteSpace(culture))
    {
        httpContext.Response.Cookies.Append(
            CookieRequestCultureProvider.DefaultCookieName,
            CookieRequestCultureProvider.MakeCookieValue(new RequestCulture(culture)),
            new CookieOptions { Expires = DateTimeOffset.UtcNow.AddYears(1) });
    }
    return Results.Redirect(redirectUri ?? "/");
});

app.UseAntiforgery();
app.MapRazorPages();
app.MapRazorComponents<App>().AddInteractiveServerRenderMode();

app.Run();

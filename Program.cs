using Blazored.SessionStorage;
using Blazored.LocalStorage;
using PicoPlus.Components;
using PicoPlus.Infrastructure.Authorization;
using PicoPlus.Infrastructure.Http;
using PicoPlus.Infrastructure.Services;
using PicoPlus.Infrastructure.State;
using PicoPlus.Services.Admin;
using PicoPlus.Services.Auth;
using PicoPlus.State.Admin;
using DotNetEnv;
using Microsoft.AspNetCore.Localization;
using System.Globalization;

var builder = WebApplication.CreateBuilder(args);

// Load .env file if it exists
var envPath = Path.Combine(Directory.GetCurrentDirectory(), ".env");
if (File.Exists(envPath))
{
    Env.Load(envPath);
}

// Configuration - Load from appsettings.json, environment variables, and user secrets
builder.Configuration
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
    .AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.json", optional: true, reloadOnChange: true)
    .AddEnvironmentVariables()
    .AddUserSecrets<Program>(optional: true);

// Configure Kestrel for production (HTTP only) and development (HTTPS)
builder.WebHost.ConfigureKestrel(options =>
{
    if (builder.Environment.IsProduction())
    {
        // Production: HTTP only on port 5000 (Liara/Docker)
        options.ListenAnyIP(5000);
    }
    // Development uses default HTTPS configuration from launchSettings.json
});

// Logging - optimized for production
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDebug();

if (builder.Environment.IsProduction())
{
    builder.Logging.AddFilter("Microsoft", LogLevel.Warning);
    builder.Logging.AddFilter("System", LogLevel.Warning);
    builder.Logging.SetMinimumLevel(LogLevel.Information);
}
else
{
    builder.Logging.SetMinimumLevel(LogLevel.Debug);
}

// Localization
builder.Services.AddLocalization(options => options.ResourcesPath = "Resources");
var supportedCultures = new[] { new CultureInfo("fa-IR"), new CultureInfo("en-US") };
builder.Services.Configure<RequestLocalizationOptions>(options =>
{
    options.DefaultRequestCulture = new RequestCulture("fa-IR");
    options.SupportedCultures = supportedCultures;
    options.SupportedUICultures = supportedCultures;
});

// Response compression for better performance
builder.Services.AddResponseCompression(options =>
{
    options.EnableForHttps = true;
});

// Razor Components
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// Storage: Session + Local (for remember-me / persistence)
builder.Services.AddBlazoredSessionStorage();
builder.Services.AddBlazoredLocalStorage();

builder.Services.AddScoped<INavigationService, NavigationService>();
builder.Services.AddSingleton<ToastService>();
builder.Services.AddScoped<PicoPlus.Infrastructure.Services.IDialogService, DialogServiceWrapper>();
builder.Services.AddScoped<PicoPlus.Infrastructure.Services.ISessionStorageService, SessionStorageServiceWrapper>();
builder.Services.AddScoped<PicoPlus.Infrastructure.Services.ILocalStorageService, LocalStorageServiceWrapper>();
builder.Services.AddSingleton<AuthenticationStateService>();

// Admin Services
builder.Services.AddScoped<AdminAuthorizationHandler>();
builder.Services.AddScoped<AdminStateService>();
builder.Services.AddScoped<AdminOwnerService>();
builder.Services.AddScoped<DashboardService>();
builder.Services.AddScoped<KanbanService>();

// Authentication Service
builder.Services.AddScoped<AuthService>();

// HttpClient with Shecan
builder.Services.AddHttpClient("HubSpot", (sp, client) =>
{
    var configuration = sp.GetRequiredService<IConfiguration>();

    client.BaseAddress = new Uri("https://api.hubapi.com");
    client.Timeout = TimeSpan.FromSeconds(30);

    var token = Environment.GetEnvironmentVariable("HUBSPOT_TOKEN")
                ?? configuration["HubSpot:Token"];

    if (!string.IsNullOrEmpty(token))
    {
        client.DefaultRequestHeaders.Add("Authorization", $"Bearer {token}");
    }

    client.DefaultRequestHeaders.Add("Accept", "application/json");
})
.ConfigurePrimaryHttpMessageHandler(() => new ShecanDnsHttpClientHandler());

// Zibal API
builder.Services.AddHttpClient<PicoPlus.Services.Identity.Zibal>((sp, client) =>
{
    client.BaseAddress = new Uri("https://api.zibal.ir");
    client.Timeout = TimeSpan.FromSeconds(30);
})
.ConfigurePrimaryHttpMessageHandler(() => new ShecanDnsHttpClientHandler());

// Liara API
builder.Services.AddHttpClient<PicoPlus.Services.Utils.LiaraApiService>((sp, client) =>
{
    client.BaseAddress = new Uri("https://api.iran.liara.ir");
    client.Timeout = TimeSpan.FromSeconds(15);
});

// CRM
builder.Services.AddScoped<PicoPlus.Services.CRM.Objects.Contact>();
builder.Services.AddScoped<PicoPlus.Services.CRM.Objects.Deal>();
builder.Services.AddScoped<PicoPlus.Services.CRM.Objects.Company>();
builder.Services.AddScoped<PicoPlus.Services.CRM.Objects.Ticket>();
builder.Services.AddScoped<PicoPlus.Services.CRM.ContactUpdateService>();

builder.Services.AddHttpClient<PicoPlus.Services.CRM.Commerce.Product>((sp, client) =>
{
    client.BaseAddress = new Uri("https://api.hubapi.com");
    client.Timeout = TimeSpan.FromSeconds(30);
})
.ConfigurePrimaryHttpMessageHandler(() => new ShecanDnsHttpClientHandler());

builder.Services.AddHttpClient<PicoPlus.Services.CRM.Commerce.LineItem>((sp, client) =>
{
    client.BaseAddress = new Uri("https://api.hubapi.com");
    client.Timeout = TimeSpan.FromSeconds(30);
})
.ConfigurePrimaryHttpMessageHandler(() => new ShecanDnsHttpClientHandler());

builder.Services.AddScoped<PicoPlus.Services.CRM.Pipelines>();
builder.Services.AddScoped<PicoPlus.Services.CRM.Associate>();
builder.Services.AddScoped<PicoPlus.Services.CRM.Owners>();

// SMS
builder.Services.AddHttpClient<PicoPlus.Services.SMS.SmsIr>((sp, client) =>
{
    client.BaseAddress = new Uri("https://api.sms.ir");
    client.Timeout = TimeSpan.FromSeconds(30);
})
.ConfigurePrimaryHttpMessageHandler(() => new ShecanDnsHttpClientHandler());

builder.Services.AddScoped<PicoPlus.Services.SMS.SMS.Send>();
builder.Services.AddScoped<PicoPlus.Services.SMS.SmsIrService>();
builder.Services.AddScoped<PicoPlus.Services.SMS.FarazSmsService>();
builder.Services.AddScoped<PicoPlus.Services.SMS.SmsServiceFactory>();
builder.Services.AddScoped<PicoPlus.Services.SMS.ISmsService, PicoPlus.Services.SMS.SmsService>();

builder.Services.AddSingleton<OtpService>();

// ViewModels
builder.Services.AddScoped<PicoPlus.ViewModels.Auth.LoginViewModel>();
builder.Services.AddScoped<PicoPlus.ViewModels.Auth.AdminLoginViewModel>();
builder.Services.AddScoped<PicoPlus.ViewModels.Auth.RegisterViewModel>();
builder.Services.AddScoped<PicoPlus.ViewModels.User.UserHomeViewModel>();
builder.Services.AddScoped<PicoPlus.ViewModels.Deal.DealCreateViewModel>();
builder.Services.AddScoped<PicoPlus.ViewModels.Deal.DealCreateDialogViewModel>();

// User Panel
builder.Services.AddSingleton<PicoPlus.Services.UserPanel.IPersianDateService, PicoPlus.Services.UserPanel.PersianDateService>();
builder.Services.AddScoped<PicoPlus.Services.UserPanel.IUserPanelService, PicoPlus.Services.UserPanel.UserPanelService>();

builder.Services.AddMemoryCache();
builder.Services.AddScoped<PicoPlus.Views.Deal.Create>();

builder.Services.AddScoped<PicoPlus.Services.Imaging.ImageProcessingService>();

builder.Services.AddRazorPages();

var app = builder.Build();

// Development logging
if (app.Environment.IsDevelopment())
{
    var logger = app.Services.GetRequiredService<ILogger<Program>>();

    logger.LogInformation("=== Configuration Sources ===");
    logger.LogInformation(".env file loaded: {EnvFileExists}", File.Exists(envPath));
    logger.LogInformation("Environment: {Environment}", app.Environment.EnvironmentName);

    var hubspotToken = Environment.GetEnvironmentVariable("HUBSPOT_TOKEN");
    var zibalToken = Environment.GetEnvironmentVariable("ZIBAL_TOKEN");

    logger.LogInformation("HubSpot Token configured: {IsConfigured} (Source: {Source})",
        !string.IsNullOrEmpty(hubspotToken),
        hubspotToken != null ? "Environment Variable" : "Configuration File");

    logger.LogInformation("Zibal Token configured: {IsConfigured} (Source: {Source})",
        !string.IsNullOrEmpty(zibalToken),
        zibalToken != null ? "Environment Variable" : "Configuration File");

    logger.LogInformation("===========================");
}

// Pipeline
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

// Localization middleware
var locOptions = app.Services.GetRequiredService<Microsoft.Extensions.Options.IOptions<RequestLocalizationOptions>>().Value;
app.UseRequestLocalization(locOptions);

// Enable response compression
app.UseResponseCompression();

// Static files with caching in production
var cacheMaxAge = app.Environment.IsProduction()
    ? TimeSpan.FromDays(30)
    : TimeSpan.FromSeconds(0);

app.UseStaticFiles(new StaticFileOptions
{
    OnPrepareResponse = ctx =>
    {
        ctx.Context.Response.Headers.Append("Cache-Control", $"public,max-age={cacheMaxAge.TotalSeconds}");
    }
});

// Culture switch endpoint
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
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();

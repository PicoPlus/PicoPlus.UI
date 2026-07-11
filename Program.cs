using Blazored.SessionStorage;
using Blazored.LocalStorage;
using PicoPlus.Components;          // App lives in PicoPlus.Components (namespace preserved)
using PicoPlus.Infrastructure.Authorization;
using PicoPlus.Infrastructure.Http; // ShecanDnsHttpClientHandler namespace (file moved, namespace preserved)
using PicoPlus.Infrastructure.Services;
using PicoPlus.Infrastructure.State;
using PicoPlus.Infrastructure.Sync;
using PicoPlus.Infrastructure.Webhooks;
using PicoPlus.Presentation.Webhooks;
using PicoPlus.Services.Admin;
using PicoPlus.Services.Auth;
using PicoPlus.Services.UserPanel;
using PicoPlus.State.Admin;
using DotNetEnv;
using Microsoft.AspNetCore.Localization;
using System.Globalization;
using PicoPlus.Localization.Extensions;

var builder = WebApplication.CreateBuilder(args);

// Load .env file if it exists (development convenience only — not for production)
var envPath = Path.Combine(Directory.GetCurrentDirectory(), ".env");
if (File.Exists(envPath))
{
    Env.Load(envPath);
}

// Configuration — sources ordered by ascending priority:
//   appsettings.json (base, no secrets) → appsettings.{env}.json → environment variables → user secrets
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

// Data Protection — platform-managed key storage and rotation.
// Keys are stored in the default location (user profile / container volume).
// Do NOT configure a persistence path to a path that is excluded from backups
// without understanding the consequences for encrypted data.
builder.Services.AddDataProtection();

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

// ── ASP.NET Core built-in localization (for middleware/cookie culture provider) ────────────────
builder.Services.AddLocalization(options => options.ResourcesPath = "Resources");
var supportedCultures = new[] { new CultureInfo("fa-IR"), new CultureInfo("en-US") };
builder.Services.Configure<RequestLocalizationOptions>(options =>
{
    options.DefaultRequestCulture = new RequestCulture("fa-IR");
    options.SupportedCultures = supportedCultures;
    options.SupportedUICultures = supportedCultures;
});

// ── PicoPlus JSON localization system ────────────────────────────────────────────────────────
builder.Services.AddPicoPlusLocalization();

// Enable hot-reload in Development for instant JSON edits
if (builder.Environment.IsDevelopment())
    builder.Services.EnableLocalizationHotReload();

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

// Zohal API
builder.Services.AddHttpClient<PicoPlus.Services.Identity.ZohalService>((sp, client) =>
{
    client.BaseAddress = new Uri("https://service.zohal.io");
    client.Timeout = TimeSpan.FromSeconds(30);
})
.ConfigurePrimaryHttpMessageHandler(() => new ShecanDnsHttpClientHandler());

// Liara API
builder.Services.AddHttpClient<PicoPlus.Services.Utils.LiaraApiService>((sp, client) =>
{
    client.BaseAddress = new Uri("https://api.iran.liara.ir");
    client.Timeout = TimeSpan.FromSeconds(15);
});

// User Panel Services
builder.Services.AddScoped<IUserPanelService, UserPanelService>();
builder.Services.AddScoped<DealDetailService>();

// CRM Services
builder.Services.AddScoped<PicoPlus.Services.CRM.Objects.Contact>();
builder.Services.AddScoped<PicoPlus.Services.CRM.Objects.Deal>();
builder.Services.AddScoped<PicoPlus.Services.CRM.Objects.Company>();
builder.Services.AddScoped<PicoPlus.Services.CRM.Objects.Ticket>();
builder.Services.AddScoped<PicoPlus.Services.CRM.Engagements.Notes>();
builder.Services.AddScoped<PicoPlus.Services.CRM.Associate>();
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
// IPPanel Edge API
builder.Services.AddHttpClient<PicoPlus.Services.SMS.IpPanelClient>((sp, client) =>
{
    client.BaseAddress = new Uri(PicoPlus.Services.SMS.IpPanelClient.BaseUrl);
    client.Timeout = TimeSpan.FromSeconds(30);
    client.DefaultRequestHeaders.Add("User-Agent", "PicoPlus-IPPanel-Client/1.0");
})
.ConfigurePrimaryHttpMessageHandler(() => new ShecanDnsHttpClientHandler());

builder.Services.AddScoped<PicoPlus.Services.SMS.SmsIrService>();
builder.Services.AddScoped<PicoPlus.Services.SMS.FarazSmsService>();
builder.Services.AddScoped<PicoPlus.Services.SMS.IpPanelSmsService>();
builder.Services.AddScoped<PicoPlus.Services.SMS.SmsServiceFactory>();
builder.Services.AddScoped<PicoPlus.Services.SMS.ISmsService, PicoPlus.Services.SMS.SmsService>();

builder.Services.AddSingleton<OtpService>();

// ── Application services (Clean Arch — no MVVM) ──────────────────────────────
builder.Services.AddScoped<PicoPlus.Services.Auth.IRegisterService,    PicoPlus.Services.Auth.RegisterService>();
builder.Services.AddScoped<PicoPlus.Services.Deal.IDealCreateService,  PicoPlus.Services.Deal.DealCreateService>();

// User Panel
builder.Services.AddSingleton<PicoPlus.Services.UserPanel.IPersianDateService, PicoPlus.Services.UserPanel.PersianDateService>();
builder.Services.AddScoped<PicoPlus.Services.UserPanel.IUserPanelService, PicoPlus.Services.UserPanel.UserPanelService>();

builder.Services.AddMemoryCache();

// ── HubSpot Webhook Infrastructure ───────────────────────────────────────────
// Registers: HubSpotWebhookOptions, HubSpotSignatureVerifier (singleton),
//            InMemoryWebhookEventQueue (singleton), WebhookDispatcherService (hosted).
builder.Services.AddHubSpotWebhooks();

// ── Event-Driven Architecture + Bidirectional Sync ────────────────────────────
// Registers: MediatR, IDomainEventDispatcher, IIntegrationEventPublisher,
//            ISyncStateRepository, BidirectionalSyncService, HubSpotWebhookSyncHandler.
builder.Services.AddEventDrivenSync();

// ── Infrastructure: concrete implementations ──────────────────────────────
builder.Services.AddScoped<PicoPlus.Services.Imaging.ImageProcessingService>();

// ── Application interfaces → Infrastructure adapters ─────────────────────
// These registrations satisfy the dependency-inversion rule:
//   Application layer depends on interfaces; Infrastructure provides implementations.
builder.Services.AddScoped<
    PicoPlus.Application.Common.Interfaces.IContactRepository,
    PicoPlus.Services.CRM.Objects.ContactRepository>();

builder.Services.AddScoped<
    PicoPlus.Application.Common.Interfaces.IDealRepository,
    PicoPlus.Services.CRM.Objects.DealRepository>();

builder.Services.AddScoped<
    PicoPlus.Application.Common.Interfaces.IOwnerRepository,
    PicoPlus.Services.CRM.OwnerRepository>();

builder.Services.AddScoped<
    PicoPlus.Application.Common.Interfaces.IPipelineRepository,
    PicoPlus.Services.CRM.PipelineRepository>();

builder.Services.AddScoped<
    PicoPlus.Application.Common.Interfaces.IAssociateService,
    PicoPlus.Services.CRM.AssociateAdapter>();

builder.Services.AddScoped<
    PicoPlus.Application.Common.Interfaces.ILineItemRepository,
    PicoPlus.Services.CRM.Commerce.LineItemRepository>();

builder.Services.AddScoped<
    PicoPlus.Application.Common.Interfaces.IIdentityVerificationService,
    PicoPlus.Services.Identity.ZohalIdentityAdapter>();

builder.Services.AddScoped<
    PicoPlus.Application.Common.Interfaces.IImageProcessingService,
    PicoPlus.Services.Imaging.ImageProcessingAdapter>();

builder.Services.AddRazorPages();

var app = builder.Build();

// Development startup diagnostics — presence only, never values
if (app.Environment.IsDevelopment())
{
    var logger = app.Services.GetRequiredService<ILogger<Program>>();
    var config = app.Services.GetRequiredService<IConfiguration>();

    logger.LogInformation("=== Configuration Sources ===");
    logger.LogInformation(".env file loaded: {EnvFileExists}", File.Exists(envPath));
    logger.LogInformation("Environment: {Environment}", app.Environment.EnvironmentName);
    logger.LogInformation("HubSpot Token configured: {IsConfigured}", !string.IsNullOrEmpty(config["HubSpot:Token"]));
    logger.LogInformation("Zohal Token configured: {IsConfigured}",   !string.IsNullOrEmpty(config["Zohal:Token"]));
    logger.LogInformation("IPPanel ApiKey configured: {IsConfigured}", !string.IsNullOrEmpty(config["IPPanel:ApiKey"]));
    logger.LogInformation("Liara ApiToken configured: {IsConfigured}", !string.IsNullOrEmpty(config["Liara:ApiToken"]));
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

// ── IPPanel diagnostics (development only) ───────────────────────────────────
// GET /diag/ippanel  — validates the configured API key against IPPanel
if (app.Environment.IsDevelopment())
{
    app.MapGet("/diag/ippanel", async (PicoPlus.Services.SMS.IpPanelClient client) =>
    {
        var result = await client.CheckTokenAsync();
        return Results.Json(result);
    });
}

// ── HubSpot Webhook Endpoint ──────────────────────────────────────────────────
// POST /webhooks/hubspot  — antiforgery disabled (HMAC-SHA256 v3 signature used instead)
app.MapHubSpotWebhook();

app.MapRazorPages();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();

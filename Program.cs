using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;
using Blazored.SessionStorage;
using Blazored.LocalStorage;
using NovinCRM.Components;          // App lives in NovinCRM.Components (namespace preserved)
using NovinCRM.Infrastructure.Authorization;
using NovinCRM.Infrastructure.Http; // ShecanDnsHttpClientHandler namespace (file moved, namespace preserved)
using NovinCRM.Infrastructure.Logging;
using NovinCRM.Infrastructure.Services;
using NovinCRM.Infrastructure.State;
using NovinCRM.Infrastructure.Sync;
using NovinCRM.Infrastructure.Webhooks;
using NovinCRM.Presentation.Webhooks;
using Microsoft.EntityFrameworkCore;
using NovinCRM.Application.Features.CRM.Commerce;
using NovinCRM.Application.Features.CRM.EventHandlers;
using NovinCRM.Infrastructure.Backup;
using NovinCRM.Infrastructure.HubSpot.CRM.Commerce;
using NovinCRM.Infrastructure.Persistence;
using NovinCRM.Services.Admin;
using NovinCRM.Services.Auth;
using NovinCRM.Services.UserPanel;
using NovinCRM.State.Admin;
using DotNetEnv;
using Microsoft.AspNetCore.Localization;
using System.Globalization;
using NovinCRM.Localization.Extensions;

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

// ── Sentry — error monitoring, tracing, and structured logs ──────────────────
builder.WebHost.UseSentry(o =>
{
    o.Dsn = "https://3c72551a13dba31fa8fea00b077170cc@o4508578883895296.ingest.de.sentry.io/4511738441498704";
    // Disable verbose SDK debug output in production
    o.Debug = !builder.Environment.IsProduction();
    // Capture 100% of transactions for tracing (tune down in production as needed)
    o.TracesSampleRate = 1.0;
    // Forward ASP.NET Core log entries to Sentry
    o.EnableLogs = true;
});

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

// ── NovinCRM JSON localization system ────────────────────────────────────────────────────────
builder.Services.AddNovinCRMLocalization();

// Enable hot-reload in Development for instant JSON edits
if (builder.Environment.IsDevelopment())
    builder.Services.EnableLocalizationHotReload();

// Response compression for better performance
builder.Services.AddResponseCompression(options =>
{
    options.EnableForHttps = true;
});

// ── Correlation ID service ─────────────────────────────────────────────────
// Singleton: the AsyncLocal<> inside CorrelationIdService is per-execution-context,
// so one singleton instance works correctly across all concurrent requests.
builder.Services.AddSingleton<CorrelationIdService>();

// Razor Components
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// Storage: Session + Local (for remember-me / persistence)
builder.Services.AddBlazoredSessionStorage();
builder.Services.AddBlazoredLocalStorage();

builder.Services.AddScoped<INavigationService, NavigationService>();
builder.Services.AddSingleton<ToastService>();
builder.Services.AddScoped<NovinCRM.Infrastructure.Services.IDialogService, DialogServiceWrapper>();
builder.Services.AddScoped<NovinCRM.Infrastructure.Services.ISessionStorageService, SessionStorageServiceWrapper>();
builder.Services.AddScoped<NovinCRM.Infrastructure.Services.ILocalStorageService, LocalStorageServiceWrapper>();
// AuthenticationStateService — scoped: each Blazor circuit gets its own instance
// with its own in-memory state, backed by IDistributedCache for cross-restart durability.
builder.Services.AddScoped<AuthenticationStateService>();

// Admin Services
builder.Services.AddScoped<AdminAuthorizationHandler>();
builder.Services.AddScoped<AdminStateService>();
builder.Services.AddScoped<AdminOwnerService>();
builder.Services.AddScoped<DashboardService>();
builder.Services.AddScoped<KanbanService>();

// Authentication Service
builder.Services.AddScoped<AuthService>();

// ── HubSpot token validation — fail-fast on startup ─────────────────────
// Resolved once, before DI container is built, so a missing token causes an
// immediate startup error rather than a confusing 401 at runtime.
var hubSpotToken = Environment.GetEnvironmentVariable("HUBSPOT_TOKEN")
    ?? builder.Configuration["HubSpot:Token"]
    ?? throw new InvalidOperationException(
        "HubSpot:Token is required. Set the HUBSPOT_TOKEN environment variable or " +
        "HubSpot:Token in configuration.");

// HttpClient with Shecan
builder.Services.AddHttpClient("HubSpot", (sp, client) =>
{
    client.BaseAddress = new Uri("https://api.hubapi.com");
    client.Timeout = TimeSpan.FromSeconds(30);

    // Authorization header value is set but never written to logs
    // (SanitizingLoggingHandler strips it before any log sink sees it).
    client.DefaultRequestHeaders.Add("Authorization", $"Bearer {hubSpotToken}");
    client.DefaultRequestHeaders.Add("Accept", "application/json");
})
.ConfigurePrimaryHttpMessageHandler(() => new ShecanDnsHttpClientHandler())
.AddHttpMessageHandler<SanitizingLoggingHandler>();

// Register the sanitizing handler so DI can inject it
builder.Services.AddTransient<SanitizingLoggingHandler>();

// Zohal API
builder.Services.AddHttpClient<NovinCRM.Services.Identity.ZohalService>((sp, client) =>
{
    client.BaseAddress = new Uri("https://service.zohal.io");
    client.Timeout = TimeSpan.FromSeconds(30);
})
.ConfigurePrimaryHttpMessageHandler(() => new ShecanDnsHttpClientHandler());

// Liara API
builder.Services.AddHttpClient<NovinCRM.Services.Utils.LiaraApiService>((sp, client) =>
{
    client.BaseAddress = new Uri("https://api.iran.liara.ir");
    client.Timeout = TimeSpan.FromSeconds(15);
});

// User Panel Services
builder.Services.AddScoped<IUserPanelService, UserPanelService>();
builder.Services.AddScoped<DealDetailService>();

// CRM Services
builder.Services.AddScoped<NovinCRM.Services.CRM.Objects.Contact>();
builder.Services.AddScoped<NovinCRM.Services.CRM.Objects.Deal>();
builder.Services.AddScoped<NovinCRM.Services.CRM.Objects.Company>();
builder.Services.AddScoped<NovinCRM.Services.CRM.Objects.Ticket>();
builder.Services.AddScoped<NovinCRM.Services.CRM.Engagements.Notes>();
builder.Services.AddScoped<NovinCRM.Services.CRM.Associate>();
builder.Services.AddScoped<NovinCRM.Services.CRM.ContactUpdateService>();

builder.Services.AddHttpClient<NovinCRM.Services.CRM.Commerce.Product>((sp, client) =>
{
    client.BaseAddress = new Uri("https://api.hubapi.com");
    client.Timeout = TimeSpan.FromSeconds(30);
})
.ConfigurePrimaryHttpMessageHandler(() => new ShecanDnsHttpClientHandler());

builder.Services.AddHttpClient<NovinCRM.Services.CRM.Commerce.LineItem>((sp, client) =>
{
    client.BaseAddress = new Uri("https://api.hubapi.com");
    client.Timeout = TimeSpan.FromSeconds(30);
})
.ConfigurePrimaryHttpMessageHandler(() => new ShecanDnsHttpClientHandler());

builder.Services.AddScoped<NovinCRM.Services.CRM.Pipelines>();
builder.Services.AddScoped<NovinCRM.Services.CRM.Associate>();
builder.Services.AddScoped<NovinCRM.Services.CRM.Owners>();

// SMS
builder.Services.AddHttpClient<NovinCRM.Services.SMS.SmsIr>((sp, client) =>
{
    client.BaseAddress = new Uri("https://api.sms.ir");
    client.Timeout = TimeSpan.FromSeconds(30);
})
.ConfigurePrimaryHttpMessageHandler(() => new ShecanDnsHttpClientHandler());

builder.Services.AddScoped<NovinCRM.Services.SMS.SMS.Send>();
// IPPanel Edge API
builder.Services.AddHttpClient<NovinCRM.Services.SMS.IpPanelClient>((sp, client) =>
{
    client.BaseAddress = new Uri(NovinCRM.Services.SMS.IpPanelClient.BaseUrl);
    client.Timeout = TimeSpan.FromSeconds(30);
    client.DefaultRequestHeaders.Add("User-Agent", "NovinCRM-IPPanel-Client/1.0");
})
.ConfigurePrimaryHttpMessageHandler(() => new ShecanDnsHttpClientHandler());

// Register each provider as both its concrete type (for HttpClient wiring) AND
// as a keyed ISmsService so SmsServiceFactory can resolve by config string key.
builder.Services.AddScoped<NovinCRM.Services.SMS.SmsIrService>();
builder.Services.AddScoped<NovinCRM.Services.SMS.FarazSmsService>();
builder.Services.AddScoped<NovinCRM.Services.SMS.IpPanelSmsService>();

builder.Services.AddKeyedScoped<NovinCRM.Services.SMS.ISmsService,
    NovinCRM.Services.SMS.SmsIrService>(NovinCRM.Services.SMS.SmsServiceFactory.KeySmsIr);
builder.Services.AddKeyedScoped<NovinCRM.Services.SMS.ISmsService,
    NovinCRM.Services.SMS.FarazSmsService>(NovinCRM.Services.SMS.SmsServiceFactory.KeyFarazSms);
builder.Services.AddKeyedScoped<NovinCRM.Services.SMS.ISmsService,
    NovinCRM.Services.SMS.IpPanelSmsService>(NovinCRM.Services.SMS.SmsServiceFactory.KeyIpPanel);

builder.Services.AddScoped<NovinCRM.Services.SMS.SmsServiceFactory>();
builder.Services.AddScoped<NovinCRM.Services.SMS.ISmsService, NovinCRM.Services.SMS.SmsService>();

builder.Services.AddScoped<OtpService>();

// ── Application services (Clean Arch — no MVVM) ──────────────────────────────
builder.Services.AddScoped<NovinCRM.Services.Auth.IRegisterService,    NovinCRM.Services.Auth.RegisterService>();
builder.Services.AddScoped<NovinCRM.Services.Deal.IDealCreateService,  NovinCRM.Services.Deal.DealCreateService>();

// User Panel
builder.Services.AddSingleton<NovinCRM.Services.UserPanel.IPersianDateService, NovinCRM.Services.UserPanel.PersianDateService>();
builder.Services.AddScoped<NovinCRM.Services.UserPanel.IUserPanelService, NovinCRM.Services.UserPanel.UserPanelService>();

builder.Services.AddMemoryCache();

// ── Distributed cache — Redis in production, in-process fallback in dev ──────
// Set REDIS_CONNECTION_STRING env var (or ConnectionStrings:Redis in config)
// to activate the Redis-backed IDistributedCache. Without it the app falls back
// to an in-process store that is sufficient for single-instance development.
var redisCs =
    Environment.GetEnvironmentVariable("REDIS_CONNECTION_STRING")
    ?? builder.Configuration.GetConnectionString("Redis");

if (!string.IsNullOrWhiteSpace(redisCs))
    builder.Services.AddStackExchangeRedisCache(o => o.Configuration = redisCs);
else
    builder.Services.AddDistributedMemoryCache();

// ── Rate limiting (ASP.NET Core built-in — no extra package needed) ───────────
// Protects OTP send, OTP verify, and admin login from brute-force / SMS-pumping.
builder.Services.AddRateLimiter(opts =>
{
    opts.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

    // OTP send: max 3 per phone per 60 s (keyed by remote IP as a safe default)
    opts.AddSlidingWindowLimiter("otp-send", o =>
    {
        o.PermitLimit         = 3;
        o.Window              = TimeSpan.FromMinutes(1);
        o.SegmentsPerWindow   = 6;
        o.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
        o.QueueLimit          = 0;
    });

    // OTP verify: max 10 attempts per IP per 10 min
    opts.AddSlidingWindowLimiter("otp-verify", o =>
    {
        o.PermitLimit         = 10;
        o.Window              = TimeSpan.FromMinutes(10);
        o.SegmentsPerWindow   = 5;
        o.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
        o.QueueLimit          = 0;
    });

    // Admin login: max 10 per IP per 15 min
    opts.AddSlidingWindowLimiter("admin-login", o =>
    {
        o.PermitLimit         = 10;
        o.Window              = TimeSpan.FromMinutes(15);
        o.SegmentsPerWindow   = 5;
        o.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
        o.QueueLimit          = 0;
    });
});

// ── Dead-letter store — in-memory singleton (swap for Redis/SQL implementation) ──
builder.Services.AddSingleton<
    NovinCRM.Application.Common.Interfaces.IDeadLetterStore,
    NovinCRM.Infrastructure.Webhooks.InMemoryDeadLetterStore>();

// ── HubSpot Webhook Infrastructure ───────────────────────────────────────────
// Registers: HubSpotWebhookOptions, HubSpotSignatureVerifier (singleton),
//            InMemoryWebhookEventQueue (singleton), WebhookDispatcherService (hosted).
builder.Services.AddHubSpotWebhooks();

// ── Event-Driven Architecture + Bidirectional Sync ────────────────────────────
// Registers: MediatR, IDomainEventDispatcher, IIntegrationEventPublisher,
//            ISyncStateRepository (Redis or in-memory), BidirectionalSyncService, HubSpotWebhookSyncHandler.
builder.Services.AddEventDrivenSync(builder.Configuration);

// ── Infrastructure: concrete implementations ──────────────────────────────
builder.Services.AddScoped<NovinCRM.Services.Imaging.ImageProcessingService>();

// ── Application interfaces → Infrastructure adapters ─────────────────────
// These registrations satisfy the dependency-inversion rule:
//   Application layer depends on interfaces; Infrastructure provides implementations.
builder.Services.AddScoped<
    NovinCRM.Application.Common.Interfaces.IContactRepository,
    NovinCRM.Services.CRM.Objects.ContactRepository>();

builder.Services.AddScoped<
    NovinCRM.Application.Common.Interfaces.IDealRepository,
    NovinCRM.Services.CRM.Objects.DealRepository>();

builder.Services.AddScoped<
    NovinCRM.Application.Common.Interfaces.IOwnerRepository,
    NovinCRM.Services.CRM.OwnerRepository>();

builder.Services.AddScoped<
    NovinCRM.Application.Common.Interfaces.IPipelineRepository,
    NovinCRM.Services.CRM.PipelineRepository>();

builder.Services.AddScoped<
    NovinCRM.Application.Common.Interfaces.IAssociateService,
    NovinCRM.Services.CRM.AssociateAdapter>();

builder.Services.AddScoped<
    NovinCRM.Application.Common.Interfaces.ILineItemRepository,
    NovinCRM.Services.CRM.Commerce.LineItemRepository>();

// ── Invoice sub-system (issue #96) ───────────────────────────────────────────
builder.Services.AddHttpClient<HubSpotInvoiceClient>((sp, client) =>
{
    client.BaseAddress = new Uri("https://api.hubapi.com");
    client.Timeout     = TimeSpan.FromSeconds(30);
})
.ConfigurePrimaryHttpMessageHandler(() => new ShecanDnsHttpClientHandler());

builder.Services.AddScoped<
    NovinCRM.Application.Common.Interfaces.IInvoiceService,
    InvoiceService>();

builder.Services.AddSingleton<
    NovinCRM.Application.Common.Interfaces.IInvoiceAccessTokenRepository,
    NovinCRM.Infrastructure.Services.InMemoryInvoiceAccessTokenRepository>();

builder.Services.AddScoped<
    NovinCRM.Application.Common.Interfaces.IIdentityVerificationService,
    NovinCRM.Services.Identity.ZohalIdentityAdapter>();

builder.Services.AddScoped<
    NovinCRM.Application.Common.Interfaces.IImageProcessingService,
    NovinCRM.Services.Imaging.ImageProcessingAdapter>();

// ── Health checks ─────────────────────────────────────────────────────────────
// /health/live  — liveness: process is running (no dependencies checked)
// /health/ready — readiness: webhook queue depth + memory
builder.Services.AddHealthChecks()
    .AddCheck<NovinCRM.Infrastructure.Health.WebhookQueueHealthCheck>("webhook-queue")
    .AddCheck("memory", () =>
    {
        var bytes = GC.GetTotalMemory(forceFullCollection: false);
        const long limit = 512L * 1024 * 1024; // 512 MB
        return bytes < limit
            ? Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult.Healthy($"{bytes / 1024 / 1024} MB")
            : Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult.Degraded($"High memory: {bytes / 1024 / 1024} MB");
    });

builder.Services.AddRazorPages();

// ── Nightly Backup sub-system (issue #97) ────────────────────────────────────
var backupCs = builder.Configuration.GetConnectionString("BackupDb");
if (!string.IsNullOrWhiteSpace(backupCs))
{
    builder.Services.AddDbContextFactory<NovinBackupDbContext>(opts =>
        opts.UseSqlServer(backupCs));

    builder.Services.AddScoped<
        NovinCRM.Application.Common.Interfaces.IHubSpotBackupService,
        HubSpotBackupService>();

    builder.Services.AddSingleton<
        NovinCRM.Application.Common.Interfaces.IMaintenanceModeService,
        NovinCRM.Infrastructure.Services.MaintenanceModeService>();

    builder.Services.AddHostedService<NightlyBackupHostedService>();
}
else
{
    // Register a no-op maintenance service so DI doesn't fail when backup is disabled
    builder.Services.AddSingleton<
        NovinCRM.Application.Common.Interfaces.IMaintenanceModeService,
        NovinCRM.Infrastructure.Services.MaintenanceModeService>();
}

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

// ── Correlation-ID middleware — assigns / echoes X-Correlation-Id header ──
app.UseMiddleware<CorrelationIdMiddleware>();

// Rate limiting middleware — must come before endpoint routing
app.UseRateLimiter();

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
    app.MapGet("/diag/ippanel", async (NovinCRM.Services.SMS.IpPanelClient client) =>
    {
        var result = await client.CheckTokenAsync();
        return Results.Json(result);
    });
}

// ── Health check endpoints ────────────────────────────────────────────────────
// Liveness — just proves the process is alive (no dependency checks)
app.MapHealthChecks("/health/live", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
{
    Predicate = _ => false   // skip all named checks — instant 200
});
// Readiness — runs webhook-queue + memory checks
app.MapHealthChecks("/health/ready");

// ── HubSpot Webhook Endpoint ──────────────────────────────────────────────────
// POST /webhooks/hubspot  — antiforgery disabled (HMAC-SHA256 v3 signature used instead)
app.MapHubSpotWebhook();

app.MapRazorPages();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();

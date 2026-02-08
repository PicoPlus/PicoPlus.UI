using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using PicoPlus.Application.Abstractions.Auth;
using PicoPlus.Application.Abstractions.Services;
using PicoPlus.Application.Abstractions.UserPanel;
using PicoPlus.Infrastructure.Authorization;
using PicoPlus.Infrastructure.Http;
using PicoPlus.Infrastructure.Services;
using PicoPlus.Infrastructure.State;
using PicoPlus.Services.Admin;
using PicoPlus.Services.Auth;
using PicoPlus.State.Admin;

namespace PicoPlus.Infrastructure.DependencyInjection;

public static class InfrastructureServiceCollectionExtensions
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddScoped<INavigationService, NavigationService>();
        services.AddSingleton<ToastService>();
        services.AddScoped<IDialogService, DialogServiceWrapper>();
        services.AddScoped<ISessionStorageService, SessionStorageServiceWrapper>();
        services.AddScoped<ILocalStorageService, LocalStorageServiceWrapper>();
        services.AddSingleton<AuthenticationStateService>();

        services.AddScoped<AdminAuthorizationHandler>();
        services.AddScoped<AdminStateService>();
        services.AddScoped<AdminOwnerService>();
        services.AddScoped<DashboardService>();
        services.AddScoped<KanbanService>();

        services.AddScoped<IAuthService, AuthService>();

        services.AddHttpClient("HubSpot", (sp, client) =>
        {
            client.BaseAddress = new Uri("https://api.hubapi.com");
            client.Timeout = TimeSpan.FromSeconds(30);

            var token = Environment.GetEnvironmentVariable("HUBSPOT_TOKEN")
                        ?? configuration["HubSpot:Token"];
            if (!string.IsNullOrEmpty(token))
            {
                client.DefaultRequestHeaders.Add("Authorization", $"Bearer {token}");
            }
            client.DefaultRequestHeaders.Add("Accept", "application/json");
        }).ConfigurePrimaryHttpMessageHandler(() => new ShecanDnsHttpClientHandler());

        services.AddHttpClient<PicoPlus.Services.Identity.Zibal>((sp, client) =>
        {
            client.BaseAddress = new Uri("https://api.zibal.ir");
            client.Timeout = TimeSpan.FromSeconds(30);
        }).ConfigurePrimaryHttpMessageHandler(() => new ShecanDnsHttpClientHandler());

        services.AddHttpClient<PicoPlus.Services.Utils.LiaraApiService>((sp, client) =>
        {
            client.BaseAddress = new Uri("https://api.iran.liara.ir");
            client.Timeout = TimeSpan.FromSeconds(15);
        });

        services.AddScoped<PicoPlus.Services.CRM.Objects.Contact>();
        services.AddScoped<PicoPlus.Services.CRM.Objects.Deal>();
        services.AddScoped<PicoPlus.Services.CRM.Objects.Company>();
        services.AddScoped<PicoPlus.Services.CRM.Objects.Ticket>();
        services.AddScoped<PicoPlus.Services.CRM.ContactUpdateService>();

        services.AddHttpClient<PicoPlus.Services.CRM.Commerce.Product>((sp, client) =>
        {
            client.BaseAddress = new Uri("https://api.hubapi.com");
            client.Timeout = TimeSpan.FromSeconds(30);
        }).ConfigurePrimaryHttpMessageHandler(() => new ShecanDnsHttpClientHandler());

        services.AddHttpClient<PicoPlus.Services.CRM.Commerce.LineItem>((sp, client) =>
        {
            client.BaseAddress = new Uri("https://api.hubapi.com");
            client.Timeout = TimeSpan.FromSeconds(30);
        }).ConfigurePrimaryHttpMessageHandler(() => new ShecanDnsHttpClientHandler());

        services.AddScoped<PicoPlus.Services.CRM.Pipelines>();
        services.AddScoped<PicoPlus.Services.CRM.Associate>();
        services.AddScoped<PicoPlus.Services.CRM.Owners>();

        services.AddHttpClient<PicoPlus.Services.SMS.SmsIr>((sp, client) =>
        {
            client.BaseAddress = new Uri("https://api.sms.ir");
            client.Timeout = TimeSpan.FromSeconds(30);
        }).ConfigurePrimaryHttpMessageHandler(() => new ShecanDnsHttpClientHandler());

        services.AddScoped<PicoPlus.Services.SMS.SMS.Send>();
        services.AddScoped<PicoPlus.Services.SMS.SmsIrService>();
        services.AddScoped<PicoPlus.Services.SMS.FarazSmsService>();
        services.AddScoped<PicoPlus.Services.SMS.SmsServiceFactory>();
        services.AddScoped<PicoPlus.Services.SMS.ISmsService, PicoPlus.Services.SMS.SmsService>();

        services.AddSingleton<OtpService>();

        services.AddSingleton<IPersianDateService, PicoPlus.Services.UserPanel.PersianDateService>();
        services.AddScoped<IUserPanelService, PicoPlus.Services.UserPanel.UserPanelService>();

        services.AddMemoryCache();
        services.AddScoped<PicoPlus.Services.Imaging.ImageProcessingService>();

        return services;
    }
}

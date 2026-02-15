using PicoPlus.Features.UserHome.Application.DTOs;
using PicoPlus.Features.UserHome.Domain;
using PicoPlus.Features.UserHome.Infrastructure;
using PicoPlus.Infrastructure.Services;
using PicoPlus.Infrastructure.State;
using PicoPlus.Models.CRM.Commerce;
using PicoPlus.Models.CRM.Objects;
using PicoPlus.Services.Identity;
using System.Globalization;

namespace PicoPlus.Features.UserHome.Application;

public sealed class UserHomeService(
    IHubSpotClient hubSpotClient,
    ISessionStorageService sessionStorage,
    IDialogService dialogService,
    INavigationService navigationService,
    AuthenticationStateService authState,
    Zibal zibalService,
    ILogger<UserHomeService> logger) : IUserHomeService
{
    private static readonly string[] ContactProperties =
    [
        "firstname", "lastname", "email", "phone", "natcode",
        "dateofbirth", "father_name", "gender", "shahkar_status",
        "wallet", "total_revenue", "num_associated_deals", "contact_plan", "last_products_bought_product_1_image_url"
    ];

    public async Task<UserHomeDto?> InitializeAsync(CancellationToken cancellationToken = default)
    {
        var contact = await sessionStorage.GetItemAsync<Contact.Search.Response.Result>("ContactModel", cancellationToken);
        if (contact?.id is null)
        {
            navigationService.NavigateTo("/auth/login");
            return null;
        }

        authState.SetAuthenticatedUser(contact);
        return await BuildDtoAsync(contact, cancellationToken);
    }

    public async Task<UserHomeDto?> RefreshAsync(CancellationToken cancellationToken = default)
    {
        var contact = await sessionStorage.GetItemAsync<Contact.Search.Response.Result>("ContactModel", cancellationToken);
        return contact?.id is null ? null : await BuildDtoAsync(contact, cancellationToken);
    }

    public async Task SignOutAsync(CancellationToken cancellationToken = default)
    {
        await sessionStorage.RemoveItemAsync("LogInState", cancellationToken);
        await sessionStorage.RemoveItemAsync("ContactModel", cancellationToken);
        await sessionStorage.RemoveItemAsync("UserRole", cancellationToken);
        authState.ClearAuthentication();
        navigationService.NavigateTo("/auth/login");
    }

    public async Task ChangeMobileAsync(string newMobile, CancellationToken cancellationToken = default)
    {
        var contact = await sessionStorage.GetItemAsync<Contact.Search.Response.Result>("ContactModel", cancellationToken);
        if (contact?.id is null)
        {
            await dialogService.ShowErrorAsync("خطا", "کاربر یافت نشد");
            return;
        }

        var properties = new Dictionary<string, string> { ["phone"] = newMobile };
        await hubSpotClient.UpdateContactPropertiesAsync(contact.id, properties, cancellationToken);

        try
        {
            var shahkarResponse = await zibalService.ShahkarInquiryAsync(new Models.Services.Identity.Zibal.ShahkarInquiry.Request
            {
                mobile = newMobile,
                nationalCode = contact.properties.natcode
            });

            properties["shahkar_status"] = shahkarResponse?.result == 100 && shahkarResponse.data?.matched == true ? "100" : "101";
            await hubSpotClient.UpdateContactPropertiesAsync(contact.id, properties, cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Shahkar verification failed while changing mobile.");
        }

        var updated = await hubSpotClient.ReadContactAsync(contact.id, ContactProperties, cancellationToken);
        if (updated?.properties is not null)
        {
            contact.properties.phone = updated.properties.phone;
            contact.properties.shahkar_status = updated.properties.shahkar_status;
            await sessionStorage.SetItemAsync("ContactModel", contact, cancellationToken);
        }

        await dialogService.ShowSuccessAsync("موفق", "شماره موبایل به‌روزرسانی شد.");
    }

    public async Task CompleteBirthDateAsync(string birthDate, CancellationToken cancellationToken = default)
    {
        var contact = await sessionStorage.GetItemAsync<Contact.Search.Response.Result>("ContactModel", cancellationToken);
        if (contact?.id is null)
        {
            await dialogService.ShowErrorAsync("خطا", "کاربر یافت نشد");
            return;
        }

        var properties = new Dictionary<string, string> { ["dateofbirth"] = birthDate };
        await hubSpotClient.UpdateContactPropertiesAsync(contact.id, properties, cancellationToken);

        try
        {
            var inquiry = await zibalService.NationalIdentityInquiryAsync(new Models.Services.Identity.Zibal.NationalIdentityInquiry.Request
            {
                nationalCode = contact.properties.natcode,
                birthDate = birthDate,
                genderInquiry = true
            });

            if (inquiry?.result == 1 && inquiry.data?.matched == true)
            {
                if (!string.IsNullOrWhiteSpace(inquiry.data.fatherName)) properties["father_name"] = inquiry.data.fatherName;
                if (!string.IsNullOrWhiteSpace(inquiry.data.gender)) properties["gender"] = inquiry.data.gender;
                if (!string.IsNullOrWhiteSpace(inquiry.data.firstName)) properties["firstname"] = inquiry.data.firstName;
                if (!string.IsNullOrWhiteSpace(inquiry.data.lastName)) properties["lastname"] = inquiry.data.lastName;
                await hubSpotClient.UpdateContactPropertiesAsync(contact.id, properties, cancellationToken);
            }
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Birth date verification failed.");
        }

        contact.properties.dateofbirth = birthDate;
        await sessionStorage.SetItemAsync("ContactModel", contact, cancellationToken);
        await dialogService.ShowSuccessAsync("موفق", "تاریخ تولد ثبت شد.");
    }

    public async Task<IReadOnlyList<LineItem.Read.Response>> LoadDealLineItemsAsync(string dealId, CancellationToken cancellationToken = default)
    {
        var associations = await hubSpotClient.ListAssociationsAsync(dealId, "deals", "line_items", cancellationToken);
        if (associations?.results is null || associations.results.Count == 0)
        {
            return [];
        }

        var list = new List<LineItem.Read.Response>();
        foreach (var assoc in associations.results)
        {
            var lineItem = await hubSpotClient.GetLineItemAsync(assoc.toObjectId.ToString(), cancellationToken);
            if (lineItem is not null) list.Add(lineItem);
        }

        return list;
    }

    private async Task<UserHomeDto> BuildDtoAsync(Contact.Search.Response.Result contact, CancellationToken cancellationToken)
    {
        var associations = await hubSpotClient.ListAssociationsAsync(contact.id, "contact", "deals", cancellationToken);
        var dealIds = associations?.results?.Select(x => x.toObjectId.ToString()).ToArray() ?? [];

        List<Deal.GetBatch.Response.Result> rawDeals = [];
        if (dealIds.Length > 0)
        {
            var dealsResponse = await hubSpotClient.GetDealsBatchAsync(dealIds, cancellationToken);
            rawDeals = dealsResponse?.results?.OrderByDescending(x => x.createdAt).ToList() ?? [];
        }

        var deals = rawDeals.Select(x => new UserDeal(x)).ToList();
        var closedDeals = deals.Count(x => x.Stage.Contains("closed", StringComparison.OrdinalIgnoreCase));
        var totalRevenue = deals.Where(x => x.Stage.Contains("closedwon", StringComparison.OrdinalIgnoreCase)).Sum(x => x.Amount);
        var profile = new UserHomeProfile(
            contact.id,
            contact.properties?.firstname ?? string.Empty,
            contact.properties?.lastname ?? string.Empty,
            contact.properties?.phone ?? "-",
            contact.properties?.natcode ?? "-",
            contact.properties?.email ?? "-",
            contact.properties?.dateofbirth ?? "-",
            contact.properties?.father_name ?? "-",
            contact.properties?.gender ?? "-",
            contact.properties?.shahkar_status ?? "0",
            contact.properties?.wallet ?? "0",
            contact.properties?.last_products_bought_product_1_image_url ?? string.Empty);

        var metrics = new UserHomeMetrics(deals.Count, closedDeals, deals.Count - closedDeals, totalRevenue, profile.WalletAmount);
        return new UserHomeDto(profile, contact, deals, metrics);
    }

    public static string FormatNumber(decimal value) => string.Format(CultureInfo.InvariantCulture, "{0:N0}", value).Replace(",", "٬");
}

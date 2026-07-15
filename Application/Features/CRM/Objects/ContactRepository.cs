#nullable enable

using NovinCRM.Application.Common.Interfaces;
using DomainContact = NovinCRM.Domain.Entities.Contact;

namespace NovinCRM.Services.CRM.Objects;

/// <summary>
/// Implements IContactRepository by delegating to the existing Contact HTTP service.
/// Uses a type alias to disambiguate from the service class named Contact in the same namespace.
/// </summary>
public class ContactRepository : IContactRepository
{
    private readonly Contact _contact;
    private readonly ContactUpdateService _updateService;

    public ContactRepository(Contact contact, ContactUpdateService updateService)
    {
        _contact       = contact;
        _updateService = updateService;
    }

    public async Task<DomainContact?> FindByNationalCodeAsync(string nationalCode)
    {
        var result = await _contact.Search("", "ncode", nationalCode,
            ["firstname", "lastname", "ncode", "phone", "email",
             "dateofbirth", "father_name", "gender", "shahkar_status",
             "wallet", "total_revenue", "num_associated_deals",
             "contact_plan", "last_products_bought_product_1_image_url"]);

        if (result?.results == null || result.total == 0) return null;
        var r = result.results[0];
        // Search.Response.Result.Properties inherits Read.Response.Properties
        return Map(r.id, r.properties);
    }

    public async Task<DomainContact?> GetByIdAsync(string id)
    {
        var r = await _contact.Read(id);
        if (r?.properties == null) return null;
        // Read.Response.Properties inherits Base.Response.Properties — cast via base
        var p = r.properties as Models.CRM.Objects.Contact.Base.Response.Properties
                ?? r.properties;
        return new DomainContact
        {
            Id                  = r.id,
            FirstName           = p.firstname ?? string.Empty,
            LastName            = p.lastname  ?? string.Empty,
            NationalCode        = p.ncode,
            Phone               = p.phone ?? p.mobilephone,
            Email               = p.email,
            DateOfBirth         = p.dateofbirth,
            FatherName          = p.father_name,
            Gender              = p.gender,
            ShahkarStatus       = p.shahkar_status,
            Wallet              = decimal.TryParse(p.wallet,                 out var w)   ? w   : null,
            TotalRevenue        = decimal.TryParse(p.total_revenue,          out var tr)  ? tr  : null,
            NumAssociatedDeals  = int.TryParse(p.num_associated_deals,       out var nad) ? nad : null,
            ContactPlan         = p.contact_plan,
            AvatarUrl           = p.last_products_bought_product_1_image_url
        };
    }

    public async Task<DomainContact> CreateAsync(DomainContact contact)
    {
        var req = new Models.CRM.Objects.Contact.Create.Request
        {
            properties = new Models.CRM.Objects.Contact.Create.Request.Properties
            {
                firstname      = contact.FirstName,
                lastname       = contact.LastName,
                ncode          = contact.NationalCode,
                phone          = contact.Phone,
                email          = contact.Email,
                bdate          = contact.DateOfBirth,
                fathername     = contact.FatherName,
                gender         = contact.Gender,
                shahkar_status = contact.ShahkarStatus
            }
        };
        var resp = await _contact.Create(req);
        return contact with { Id = resp.id };
    }

    public Task UpdateAsync(string id, Dictionary<string, string> properties)
        => _contact.UpdateContactProperties(id, properties);

    public async Task<bool> DeleteAsync(string id) => await _contact.Delete(id);

    public Task<string?> UploadAvatarAsync(string contactId, byte[] imageBytes, string fileName = "avatar.jpg")
        => _contact.UploadAvatarAsync(contactId, imageBytes, fileName);

    public async Task<DomainContact> UpdateMissingFieldsAsync(DomainContact contact, CancellationToken ct = default)
    {
        // Build a minimal Search.Response.Result DTO that ContactUpdateService expects.
        var dto = new Models.CRM.Objects.Contact.Search.Response.Result
        {
            id = contact.Id,
            properties = new Models.CRM.Objects.Contact.Search.Response.Result.Properties
            {
                firstname      = contact.FirstName,
                lastname       = contact.LastName,
                ncode          = contact.NationalCode,
                phone          = contact.Phone,
                email          = contact.Email,
                dateofbirth    = contact.DateOfBirth,
                father_name    = contact.FatherName,
                gender         = contact.Gender,
                shahkar_status = contact.ShahkarStatus
            }
        };

        var refreshed = await _updateService.UpdateMissingFieldsAsync(dto, ct);

        // Map back to Domain entity
        return new DomainContact
        {
            Id                 = refreshed.id,
            FirstName          = refreshed.properties?.firstname  ?? contact.FirstName,
            LastName           = refreshed.properties?.lastname   ?? contact.LastName,
            NationalCode       = refreshed.properties?.ncode      ?? contact.NationalCode,
            Phone              = refreshed.properties?.phone      ?? contact.Phone,
            Email              = refreshed.properties?.email      ?? contact.Email,
            DateOfBirth        = refreshed.properties?.dateofbirth ?? contact.DateOfBirth,
            FatherName         = refreshed.properties?.father_name ?? contact.FatherName,
            Gender             = refreshed.properties?.gender     ?? contact.Gender,
            ShahkarStatus      = refreshed.properties?.shahkar_status ?? contact.ShahkarStatus,
            Wallet             = decimal.TryParse(refreshed.properties?.wallet,        out var w)  ? w  : contact.Wallet,
            TotalRevenue       = decimal.TryParse(refreshed.properties?.total_revenue, out var tr) ? tr : contact.TotalRevenue,
            NumAssociatedDeals = int.TryParse(refreshed.properties?.num_associated_deals, out var n) ? n : contact.NumAssociatedDeals,
            ContactPlan        = refreshed.properties?.contact_plan ?? contact.ContactPlan,
            AvatarUrl          = refreshed.properties?.last_products_bought_product_1_image_url ?? contact.AvatarUrl
        };
    }

    // ── private mapper ────────────────────────────────────────────────────
    private static DomainContact Map(string id, Models.CRM.Objects.Contact.Read.Response.Properties p) => new()
    {
        Id                  = id,
        FirstName           = p.firstname ?? string.Empty,
        LastName            = p.lastname  ?? string.Empty,
        NationalCode        = p.ncode,
        Phone               = p.phone ?? p.mobilephone,
        Email               = p.email,
        DateOfBirth         = p.dateofbirth,
        FatherName          = p.father_name,
        Gender              = p.gender,
        ShahkarStatus       = p.shahkar_status,
        Wallet              = decimal.TryParse(p.wallet,                 out var w)   ? w   : null,
        TotalRevenue        = decimal.TryParse(p.total_revenue,          out var tr)  ? tr  : null,
        NumAssociatedDeals  = int.TryParse(p.num_associated_deals,       out var nad) ? nad : null,
        ContactPlan         = p.contact_plan,
        AvatarUrl           = p.last_products_bought_product_1_image_url
    };
}

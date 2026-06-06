using ContactModel = PicoPlus.Models.CRM.Objects.Contact;

namespace PicoPlus.Services.Shared;

/// <summary>
/// Shared mapper to convert between Contact model formats.
/// Consolidates identical mapping code from ContactUpdateService and RegisterViewModel.
/// </summary>
public static class ContactModelMapper
{
    /// <summary>
    /// Map a Contact.Read.Response to a Contact.Search.Response.Result.
    /// </summary>
    public static ContactModel.Search.Response.Result ToSearchResult(ContactModel.Read.Response source)
    {
        return new ContactModel.Search.Response.Result
        {
            id = source.id,
            properties = new ContactModel.Search.Response.Result.Properties
            {
                email = source.properties.email,
                firstname = source.properties.firstname,
                lastname = source.properties.lastname,
                phone = source.properties.phone,
                natcode = source.properties.natcode,
                dateofbirth = source.properties.dateofbirth,
                father_name = source.properties.father_name,
                gender = source.properties.gender,
                total_revenue = source.properties.total_revenue,
                shahkar_status = source.properties.shahkar_status,
                wallet = source.properties.wallet,
                num_associated_deals = source.properties.num_associated_deals,
                contact_plan = source.properties.contact_plan
            },
            createdAt = source.createdAt.ToString("o"),
            updatedAt = source.updatedAt.ToString("o"),
            archived = source.archived
        };
    }

    /// <summary>
    /// Map a Contact.Create.Response to a Contact.Search.Response.Result.
    /// </summary>
    public static ContactModel.Search.Response.Result ToSearchResult(ContactModel.Create.Response source)
    {
        return new ContactModel.Search.Response.Result
        {
            id = source.id,
            properties = new ContactModel.Search.Response.Result.Properties
            {
                email = source.properties.email,
                firstname = source.properties.firstname,
                lastname = source.properties.lastname,
                phone = source.properties.phone,
                natcode = source.properties.natcode,
                dateofbirth = source.properties.dateofbirth,
                father_name = source.properties.father_name,
                gender = source.properties.gender,
                total_revenue = source.properties.total_revenue,
                shahkar_status = source.properties.shahkar_status,
                wallet = source.properties.wallet,
                num_associated_deals = source.properties.num_associated_deals,
                contact_plan = source.properties.contact_plan
            },
            createdAt = source.createdAt.ToString("o"),
            updatedAt = source.updatedAt.ToString("o"),
            archived = source.archived
        };
    }
}

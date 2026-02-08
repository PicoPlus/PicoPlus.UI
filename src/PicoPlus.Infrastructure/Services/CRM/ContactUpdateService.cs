using Microsoft.Extensions.Logging;
using PicoPlus.Models.CRM.Objects;
using PicoPlus.Services.Identity;
using ContactService = PicoPlus.Services.CRM.Objects.Contact;

namespace PicoPlus.Services.CRM;

/// <summary>
/// Service for automatically updating contact information from Zibal
/// Updates missing or null fields with data from Zibal APIs
/// </summary>
public class ContactUpdateService
{
    private readonly ContactService _contactService;
    private readonly Zibal _zibalService;
    private readonly ILogger<ContactUpdateService> _logger;

    public ContactUpdateService(
        ContactService contactService,
        Zibal zibalService,
        ILogger<ContactUpdateService> logger)
    {
        _contactService = contactService;
        _zibalService = zibalService;
        _logger = logger;
    }

    /// <summary>
    /// Check and update missing contact fields from Zibal on login
    /// </summary>
    public async Task<Contact.Search.Response.Result> UpdateMissingFieldsAsync(
        Contact.Search.Response.Result contact,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Checking missing fields for contact: {ContactId}", contact.id);

            bool needsUpdate = false;
            var properties = contact.properties;

            // Check which fields are missing
            bool missingFatherName = string.IsNullOrWhiteSpace(properties.father_name);
            bool missingBirthDate = string.IsNullOrWhiteSpace(properties.dateofbirth);
            bool missingGender = string.IsNullOrWhiteSpace(properties.gender);
            bool missingShahkarStatus = string.IsNullOrWhiteSpace(properties.shahkar_status) ||
                                       properties.shahkar_status == "0";

            _logger.LogInformation(
                "Missing fields - FatherName: {FatherName}, BirthDate: {BirthDate}, Gender: {Gender}, ShahkarStatus: {Shahkar}",
                missingFatherName, missingBirthDate, missingGender, missingShahkarStatus);

            // Update from National Identity Inquiry if personal info is missing
            if (missingFatherName || missingBirthDate || missingGender)
            {
                await UpdateFromNationalIdentityAsync(contact, cancellationToken);
                needsUpdate = true;
            }

            // Update Shahkar status if missing or not verified
            if (missingShahkarStatus && !string.IsNullOrWhiteSpace(properties.phone))
            {
                await UpdateShahkarStatusAsync(contact, cancellationToken);
                needsUpdate = true;
            }

            // Refresh contact data from HubSpot if updates were made
            if (needsUpdate)
            {
                _logger.LogInformation("Refreshing contact data from HubSpot: {ContactId}", contact.id);
                var updatedContact = await _contactService.Read(contact.id);

                // Map back to Search.Response.Result format
                return new Contact.Search.Response.Result
                {
                    id = updatedContact.id,
                    properties = new Contact.Search.Response.Result.Properties
                    {
                        email = updatedContact.properties.email,
                        firstname = updatedContact.properties.firstname,
                        lastname = updatedContact.properties.lastname,
                        phone = updatedContact.properties.phone,
                        natcode = updatedContact.properties.natcode,
                        dateofbirth = updatedContact.properties.dateofbirth,
                        father_name = updatedContact.properties.father_name,
                        gender = updatedContact.properties.gender,
                        total_revenue = updatedContact.properties.total_revenue,
                        shahkar_status = updatedContact.properties.shahkar_status,
                        wallet = updatedContact.properties.wallet,
                        num_associated_deals = updatedContact.properties.num_associated_deals,
                        contact_plan = updatedContact.properties.contact_plan
                    },
                    createdAt = updatedContact.createdAt.ToString("o"),
                    updatedAt = updatedContact.updatedAt.ToString("o"),
                    archived = updatedContact.archived
                };
            }

            return contact;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating missing fields for contact: {ContactId}", contact.id);
            // Return original contact if update fails
            return contact;
        }
    }

    /// <summary>
    /// Update contact with National Identity data from Zibal
    /// </summary>
    private async Task UpdateFromNationalIdentityAsync(
        Contact.Search.Response.Result contact,
        CancellationToken cancellationToken)
    {
        try
        {
            var natCode = contact.properties.natcode;
            var birthDate = contact.properties.dateofbirth;

            if (string.IsNullOrWhiteSpace(natCode))
            {
                _logger.LogWarning("Cannot update from Zibal: National code is missing");
                return;
            }

            // If birthdate is missing, we can't call Zibal National Identity API
            if (string.IsNullOrWhiteSpace(birthDate))
            {
                _logger.LogInformation("Cannot call Zibal: Birth date is required but missing");
                return;
            }

            _logger.LogInformation("Fetching national identity data from Zibal for: {NatCode}", natCode);

            var inquiry = await _zibalService.NationalIdentityInquiryAsync(
                new Models.Services.Identity.Zibal.NationalIdentityInquiry.Request
                {
                    nationalCode = natCode,
                    birthDate = birthDate,
                    genderInquiry = true
                });

            if (inquiry?.result == 1 && inquiry.data?.matched == true)
            {
                _logger.LogInformation("Zibal data retrieved successfully for: {NatCode}", natCode);

                var updateProperties = new Dictionary<string, string>();

                // Update father name if missing
                if (string.IsNullOrWhiteSpace(contact.properties.father_name) &&
                    !string.IsNullOrWhiteSpace(inquiry.data.fatherName))
                {
                    updateProperties["father_name"] = inquiry.data.fatherName;
                    _logger.LogInformation("Updating father_name: {FatherName}", inquiry.data.fatherName);
                }

                // Update gender if missing
                if (string.IsNullOrWhiteSpace(contact.properties.gender) &&
                    !string.IsNullOrWhiteSpace(inquiry.data.gender))
                {
                    updateProperties["gender"] = inquiry.data.gender;
                    _logger.LogInformation("Updating gender: {Gender}", inquiry.data.gender);
                }

                // Update first/last name if they seem incomplete
                if (!string.IsNullOrWhiteSpace(inquiry.data.firstName) &&
                    contact.properties.firstname != inquiry.data.firstName)
                {
                    updateProperties["firstname"] = inquiry.data.firstName;
                    _logger.LogInformation("Updating firstname: {FirstName}", inquiry.data.firstName);
                }

                if (!string.IsNullOrWhiteSpace(inquiry.data.lastName) &&
                    contact.properties.lastname != inquiry.data.lastName)
                {
                    updateProperties["lastname"] = inquiry.data.lastName;
                    _logger.LogInformation("Updating lastname: {LastName}", inquiry.data.lastName);
                }

                // Perform update if we have properties to update
                if (updateProperties.Any())
                {
                    await _contactService.UpdateContactProperties(contact.id, updateProperties);
                    _logger.LogInformation("Contact updated with {Count} properties from Zibal", updateProperties.Count);
                }
            }
            else
            {
                _logger.LogWarning("Zibal inquiry failed or not matched for: {NatCode}", natCode);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating from National Identity Inquiry");
        }
    }

    /// <summary>
    /// Update Shahkar status from Zibal
    /// </summary>
    private async Task UpdateShahkarStatusAsync(
        Contact.Search.Response.Result contact,
        CancellationToken cancellationToken)
    {
        try
        {
            var natCode = contact.properties.natcode;
            var phone = contact.properties.phone;

            if (string.IsNullOrWhiteSpace(natCode) || string.IsNullOrWhiteSpace(phone))
            {
                _logger.LogWarning("Cannot update Shahkar: National code or phone is missing");
                return;
            }

            _logger.LogInformation("Verifying Shahkar status for: {Phone}, {NatCode}", phone, natCode);

            var shahkarResponse = await _zibalService.ShahkarInquiryAsync(
                new Models.Services.Identity.Zibal.ShahkarInquiry.Request
                {
                    mobile = phone,
                    nationalCode = natCode
                });

            string shahkarStatus;

            if (shahkarResponse?.result == 100 && shahkarResponse.data?.matched == true)
            {
                shahkarStatus = "100"; // Verified
                _logger.LogInformation("Shahkar verified successfully: Status 100");
            }
            else if (shahkarResponse?.result == 100 && shahkarResponse.data?.matched == false)
            {
                shahkarStatus = "101"; // Not matched
                _logger.LogWarning("Shahkar not matched: Status 101");
            }
            else
            {
                shahkarStatus = shahkarResponse?.result?.ToString() ?? "999";
                _logger.LogWarning("Shahkar unexpected result: Status {Status}", shahkarStatus);
            }

            // Update contact with Shahkar status
            var updateProperties = new Dictionary<string, string>
            {
                ["shahkar_status"] = shahkarStatus
            };

            await _contactService.UpdateContactProperties(contact.id, updateProperties);
            _logger.LogInformation("Contact updated with Shahkar status: {Status}", shahkarStatus);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating Shahkar status");
        }
    }
}

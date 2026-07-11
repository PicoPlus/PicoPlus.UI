using Microsoft.Extensions.Logging;
using PicoPlus.Models.CRM.Objects;
using PicoPlus.Services.Identity;
using ContactService = PicoPlus.Services.CRM.Objects.Contact;

namespace PicoPlus.Services.CRM;

/// <summary>
/// Service for automatically updating contact information from Zohal.
/// Updates missing or null fields with data from Zohal APIs.
/// </summary>
public class ContactUpdateService
{
    private readonly ContactService _contactService;
    private readonly ZohalService _zohalService;
    private readonly ILogger<ContactUpdateService> _logger;

    public ContactUpdateService(
        ContactService contactService,
        ZohalService zohalService,
        ILogger<ContactUpdateService> logger)
    {
        _contactService = contactService;
        _zohalService = zohalService;
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
                        ncode = updatedContact.properties.ncode,
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
    /// Update contact with National Identity data from Zohal.
    /// </summary>
    private async Task UpdateFromNationalIdentityAsync(
        Contact.Search.Response.Result contact,
        CancellationToken cancellationToken)
    {
        try
        {
            var natCode   = contact.properties.ncode;
            var birthDate = contact.properties.dateofbirth;

            if (string.IsNullOrWhiteSpace(natCode))
            {
                _logger.LogWarning("Cannot update from Zohal: National code is missing");
                return;
            }

            if (string.IsNullOrWhiteSpace(birthDate))
            {
                _logger.LogInformation("Cannot call Zohal: Birth date is required but missing");
                return;
            }

            _logger.LogInformation("Fetching national identity data from Zohal for: {NatCode}", natCode);

            var inquiry = await _zohalService.NationalIdentityInquiryAsync(natCode, birthDate);

            if (inquiry?.Result == 1 && inquiry.ResponseBody?.Data?.Matched == true)
            {
                _logger.LogInformation("Zohal data retrieved successfully for: {NatCode}", natCode);

                var data = inquiry.ResponseBody.Data;
                var updateProperties = new Dictionary<string, string>();

                if (string.IsNullOrWhiteSpace(contact.properties.father_name) &&
                    !string.IsNullOrWhiteSpace(data.FatherName))
                {
                    updateProperties["father_name"] = data.FatherName;
                    _logger.LogInformation("Updating father_name: {FatherName}", data.FatherName);
                }

                if (!string.IsNullOrWhiteSpace(data.FirstName) &&
                    contact.properties.firstname != data.FirstName)
                {
                    updateProperties["firstname"] = data.FirstName;
                    _logger.LogInformation("Updating firstname: {FirstName}", data.FirstName);
                }

                if (!string.IsNullOrWhiteSpace(data.LastName) &&
                    contact.properties.lastname != data.LastName)
                {
                    updateProperties["lastname"] = data.LastName;
                    _logger.LogInformation("Updating lastname: {LastName}", data.LastName);
                }

                if (updateProperties.Any())
                {
                    await _contactService.UpdateContactProperties(contact.id, updateProperties);
                    _logger.LogInformation("Contact updated with {Count} properties from Zohal", updateProperties.Count);
                }
            }
            else
            {
                _logger.LogWarning("Zohal inquiry failed or not matched for: {NatCode}", natCode);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating from National Identity Inquiry");
        }
    }

    /// <summary>
    /// Update Shahkar status from Zohal.
    /// </summary>
    private async Task UpdateShahkarStatusAsync(
        Contact.Search.Response.Result contact,
        CancellationToken cancellationToken)
    {
        try
        {
            var natCode = contact.properties.ncode;
            var phone   = contact.properties.phone;

            if (string.IsNullOrWhiteSpace(natCode) || string.IsNullOrWhiteSpace(phone))
            {
                _logger.LogWarning("Cannot update Shahkar: National code or phone is missing");
                return;
            }

            _logger.LogInformation("Verifying Shahkar status for: {Phone}, {NatCode}", phone, natCode);

            var resp = await _zohalService.ShahkarInquiryAsync(natCode, phone);

            string shahkarStatus;

            if (resp?.Result == 1 && resp.ResponseBody?.Data?.Matched == true)
            {
                shahkarStatus = "100"; // Verified
                _logger.LogInformation("Shahkar verified successfully: Status 100");
            }
            else if (resp?.Result == 1 && resp.ResponseBody?.Data?.Matched == false)
            {
                shahkarStatus = "101"; // Not matched
                _logger.LogWarning("Shahkar not matched: Status 101");
            }
            else
            {
                shahkarStatus = resp?.ResponseBody?.ErrorCode ?? "999";
                _logger.LogWarning("Shahkar unexpected result: Status {Status}", shahkarStatus);
            }

            await _contactService.UpdateContactProperties(contact.id,
                new Dictionary<string, string> { ["shahkar_status"] = shahkarStatus });
            _logger.LogInformation("Contact updated with Shahkar status: {Status}", shahkarStatus);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating Shahkar status");
        }
    }
}

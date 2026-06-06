using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using PicoPlus.Services.Shared;

namespace PicoPlus.Services.CRM.Objects;

/// <summary>
/// Extension methods for Contact service to handle file uploads.
/// Note: The Contact class now has a built-in UploadAvatarAsync method.
/// This extension is retained for backward compatibility when called externally.
/// </summary>
public static class ContactFileExtensions
{
    /// <summary>
    /// Upload avatar image to HubSpot and update contact property
    /// Step 1: Upload file to HubSpot Files API
    /// Step 2: Update contact property with file URL
    /// </summary>
    public static async Task<string?> UploadAvatarAsync(
        this Contact contactService,
        IHttpClientFactory httpClientFactory,
        IConfiguration configuration,
        ILogger logger,
        string contactId,
        byte[] imageBytes,
        string fileName = "avatar.jpg")
    {
        return await contactService.UploadAvatarAsync(contactId, imageBytes, fileName);
    }
}

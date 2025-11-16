using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace PicoPlus.Services.CRM.Objects;

/// <summary>
/// Extension methods for Contact service to handle file uploads
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
        try
        {
            logger.LogInformation("Uploading avatar for contact: {ContactId}, Size: {Size}KB", contactId, imageBytes.Length / 1024);

            var httpClient = httpClientFactory.CreateClient("HubSpot");
            var hubSpotToken = Environment.GetEnvironmentVariable("HUBSPOT_TOKEN")
                              ?? configuration["HubSpot:Token"]
                              ?? throw new InvalidOperationException("HubSpot token not configured");

            // Step 1: Upload file to HubSpot Files API
            var filesUrl = "/files/v3/files";
            using var fileContent = new MultipartFormDataContent();
            using var imageContent = new ByteArrayContent(imageBytes);
            imageContent.Headers.ContentType = new MediaTypeHeaderValue("image/jpeg");

            fileContent.Add(imageContent, "file", fileName);
            fileContent.Add(new StringContent("PUBLIC_INDEXABLE"), "options");
            fileContent.Add(new StringContent($"/avatars/{contactId}"), "folderPath");

            var fileRequest = new HttpRequestMessage(HttpMethod.Post, filesUrl)
            {
                Content = fileContent
            };
            fileRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", hubSpotToken);

            using var fileResponse = await httpClient.SendAsync(fileRequest);

            if (!fileResponse.IsSuccessStatusCode)
            {
                var errorContent = await fileResponse.Content.ReadAsStringAsync();
                logger.LogError("File upload failed: {StatusCode}, Response: {Response}",
                    fileResponse.StatusCode, errorContent);
                return null;
            }

            var fileResponseJson = await fileResponse.Content.ReadAsStringAsync();
            var fileResult = JsonSerializer.Deserialize<JsonElement>(fileResponseJson);

            var fileUrl = fileResult.GetProperty("url").GetString();
            logger.LogInformation("File uploaded successfully: {FileUrl}", fileUrl);

            // Step 2: Update contact property with file URL
            if (!string.IsNullOrEmpty(fileUrl))
            {
                await contactService.UpdateContactProperties(contactId, new Dictionary<string, string>
                {
                    ["last_products_bought_product_1_image_url"] = fileUrl
                });

                logger.LogInformation("Contact avatar URL updated successfully");
                return fileUrl;
            }

            return null;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error uploading avatar for contact: {ContactId}", contactId);
            return null;
        }
    }
}

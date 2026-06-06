using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using PicoPlus.Services.Shared;

namespace PicoPlus.Services.CRM.Objects
{
    /// <summary>
    /// HubSpot Contacts API Service
    /// https://developers.hubspot.com/docs/api/crm/contacts
    /// </summary>
    public class Contact
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _configuration;
        private readonly ILogger<Contact> _logger;
        private readonly string _hubSpotToken;
        private const string BaseUrl = "/crm/v3/objects/contacts";

        private static readonly string[] DefaultReadProperties = new[]
        {
            "father_name", "dateofbirth", "natcode", "shahkar_status",
            "wallet", "total_revenue", "firstname", "lastname",
            "phone", "gender", "last_products_bought_product_1_image_url", "email"
        };

        public Contact(IHttpClientFactory httpClientFactory, IConfiguration configuration,
            ILogger<Contact> logger, HubSpotTokenProvider tokenProvider)
        {
            _httpClientFactory = httpClientFactory;
            _configuration = configuration;
            _logger = logger;
            _hubSpotToken = tokenProvider.Token;
        }

        /// <summary>
        /// Search contacts using filter criteria (Updated to HubSpot API v3 latest format)
        /// POST /crm/v3/objects/contacts/search
        /// </summary>
        public async Task<Models.CRM.Objects.Contact.Search.Response> Search(
            string query,
            string paramName,
            string paramValue,
            string[] propertiesToInclude,
            int limit = 100,
            string? after = null,
            string[]? sorts = null)
        {
            var httpClient = _httpClientFactory.CreateClient("HubSpot");
            var url = $"{BaseUrl}/search";

            var payload = new
            {
                limit = limit,
                after = after,
                sorts = sorts ?? Array.Empty<string>(),
                properties = propertiesToInclude,
                filterGroups = new[]
                {
                    new
                    {
                        filters = new[]
                        {
                            new
                            {
                                propertyName = paramName,
                                value = paramValue,
                                @operator = "EQ"
                            }
                        }
                    }
                }
            };

            return await HubSpotRequestHelper.PostAsync<Models.CRM.Objects.Contact.Search.Response>(
                httpClient, url, payload, _hubSpotToken);
        }

        /// <summary>
        /// Advanced search with multiple filters and custom operators
        /// POST /crm/v3/objects/contacts/search
        /// </summary>
        public async Task<Models.CRM.Objects.Contact.Search.Response> SearchAdvanced(
            string? query = null,
            List<ContactFilter>? filters = null,
            string[]? propertiesToInclude = null,
            int limit = 100,
            string? after = null,
            string[]? sorts = null)
        {
            var httpClient = _httpClientFactory.CreateClient("HubSpot");
            var url = $"{BaseUrl}/search";

            var filterGroups = new List<object>();
            if (filters != null && filters.Any())
            {
                filterGroups.Add(new
                {
                    filters = filters.Select(f => new
                    {
                        propertyName = f.PropertyName,
                        value = f.Value,
                        values = f.Values,
                        highValue = f.HighValue,
                        @operator = f.Operator
                    }).ToArray()
                });
            }

            var payload = new
            {
                query = query,
                limit = limit,
                after = after,
                sorts = sorts ?? Array.Empty<string>(),
                properties = propertiesToInclude ?? Array.Empty<string>(),
                filterGroups = filterGroups.ToArray()
            };

            return await HubSpotRequestHelper.PostAsync<Models.CRM.Objects.Contact.Search.Response>(
                httpClient, url, payload, _hubSpotToken);
        }

        /// <summary>
        /// Create a new contact
        /// POST /crm/v3/objects/contacts
        /// </summary>
        public async Task<Models.CRM.Objects.Contact.Create.Response> Create(
            Models.CRM.Objects.Contact.Create.Request contactInfo)
        {
            var httpClient = _httpClientFactory.CreateClient("HubSpot");
            return await HubSpotRequestHelper.PostAsync<Models.CRM.Objects.Contact.Create.Response>(
                httpClient, BaseUrl, contactInfo, _hubSpotToken);
        }

        /// <summary>
        /// Get contact by ID
        /// GET /crm/v3/objects/contacts/{contactId}
        /// </summary>
        public async Task<Models.CRM.Objects.Contact.Read.Response> Read(string id, string[]? properties = null, string[]? associations = null)
        {
            var httpClient = _httpClientFactory.CreateClient("HubSpot");
            var query = HubSpotQueryBuilder.BuildQueryString(
                properties,
                associations,
                defaultProperties: DefaultReadProperties,
                defaultAssociations: new[] { "deals" });

            var url = $"{BaseUrl}/{id}?{query}";
            return await HubSpotRequestHelper.GetAsync<Models.CRM.Objects.Contact.Read.Response>(
                httpClient, url, _hubSpotToken);
        }

        /// <summary>
        /// Update specific contact properties
        /// PATCH /crm/v3/objects/contacts/{contactId}
        /// </summary>
        public async Task UpdateContactProperties(string contactId, Dictionary<string, string> properties)
        {
            var httpClient = _httpClientFactory.CreateClient("HubSpot");
            var url = $"{BaseUrl}/{contactId}";
            await HubSpotRequestHelper.PatchAsync(httpClient, url, new { properties = properties }, _hubSpotToken);
        }

        /// <summary>
        /// Update a contact
        /// PATCH /crm/v3/objects/contacts/{contactId}
        /// </summary>
        public async Task<Models.CRM.Objects.Contact.Search.Response.Result.Properties> Update(
            string contactId,
            object updatedProperties)
        {
            var httpClient = _httpClientFactory.CreateClient("HubSpot");
            var url = $"{BaseUrl}/{contactId}";
            return await HubSpotRequestHelper.PatchAsync<Models.CRM.Objects.Contact.Search.Response.Result.Properties>(
                httpClient, url, new { properties = updatedProperties }, _hubSpotToken);
        }

        /// <summary>
        /// Delete a contact (archive)
        /// DELETE /crm/v3/objects/contacts/{contactId}
        /// </summary>
        public async Task<bool> Delete(string contactId)
        {
            var httpClient = _httpClientFactory.CreateClient("HubSpot");
            var url = $"{BaseUrl}/{contactId}";
            return await HubSpotRequestHelper.DeleteAsync(httpClient, url, _hubSpotToken);
        }

        /// <summary>
        /// Get all contacts (paginated)
        /// GET /crm/v3/objects/contacts
        /// </summary>
        public async Task<Models.CRM.Objects.Contact.Search.Response> GetAll(
            int limit = 100,
            string? after = null,
            string[]? properties = null)
        {
            var httpClient = _httpClientFactory.CreateClient("HubSpot");
            var query = HubSpotQueryBuilder.BuildPaginationQuery(limit, after, properties);
            var url = $"{BaseUrl}?{query}";

            return await HubSpotRequestHelper.GetAsync<Models.CRM.Objects.Contact.Search.Response>(
                httpClient, url, _hubSpotToken);
        }

        /// <summary>
        /// Batch create contacts
        /// POST /crm/v3/objects/contacts/batch/create
        /// </summary>
        public async Task<dynamic> BatchCreate(List<Models.CRM.Objects.Contact.Create.Request> contacts)
        {
            var httpClient = _httpClientFactory.CreateClient("HubSpot");
            var url = $"{BaseUrl}/batch/create";
            return await HubSpotRequestHelper.PostAsync<dynamic>(
                httpClient, url, new { inputs = contacts }, _hubSpotToken);
        }

        /// <summary>
        /// Batch update contacts
        /// POST /crm/v3/objects/contacts/batch/update
        /// </summary>
        public async Task<dynamic> BatchUpdate(List<object> updates)
        {
            var httpClient = _httpClientFactory.CreateClient("HubSpot");
            var url = $"{BaseUrl}/batch/update";
            return await HubSpotRequestHelper.PostAsync<dynamic>(
                httpClient, url, new { inputs = updates }, _hubSpotToken);
        }

        /// <summary>
        /// Upload avatar image to HubSpot Files API and update contact property
        /// </summary>
        public async Task<string?> UploadAvatarAsync(string contactId, byte[] imageBytes, string fileName = "avatar.jpg")
        {
            try
            {
                _logger.LogInformation("Uploading avatar for contact: {ContactId}, Size: {Size}KB", contactId, imageBytes.Length / 1024);

                var httpClient = _httpClientFactory.CreateClient("HubSpot");

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
                fileRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _hubSpotToken);

                using var fileResponse = await httpClient.SendAsync(fileRequest);

                if (!fileResponse.IsSuccessStatusCode)
                {
                    var errorContent = await fileResponse.Content.ReadAsStringAsync();
                    _logger.LogError("File upload failed: {StatusCode}, Response: {Response}",
                        fileResponse.StatusCode, errorContent);
                    return null;
                }

                var fileResponseJson = await fileResponse.Content.ReadAsStringAsync();
                var fileResult = JsonSerializer.Deserialize<JsonElement>(fileResponseJson);

                var fileUrl = fileResult.GetProperty("url").GetString();
                _logger.LogInformation("File uploaded successfully: {FileUrl}", fileUrl);

                if (!string.IsNullOrEmpty(fileUrl))
                {
                    await UpdateContactProperties(contactId, new Dictionary<string, string>
                    {
                        ["last_products_bought_product_1_image_url"] = fileUrl
                    });

                    _logger.LogInformation("Contact avatar URL updated successfully");
                    return fileUrl;
                }

                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error uploading avatar for contact: {ContactId}", contactId);
                return null;
            }
        }
    }

    /// <summary>
    /// Filter model for advanced contact search
    /// </summary>
    public class ContactFilter
    {
        /// <summary>Property name to filter on</summary>
        public string PropertyName { get; set; } = string.Empty;

        /// <summary>Single value for operators like EQ, NEQ, LT, GT</summary>
        public string? Value { get; set; }

        /// <summary>Multiple values for operators like IN, NOT_IN</summary>
        public string[]? Values { get; set; }

        /// <summary>High value for BETWEEN operator</summary>
        public string? HighValue { get; set; }

        /// <summary>
        /// Operator: EQ, NEQ, LT, LTE, GT, GTE, BETWEEN, IN, NOT_IN, HAS_PROPERTY, NOT_HAS_PROPERTY, CONTAINS_TOKEN, NOT_CONTAINS_TOKEN
        /// </summary>
        public string Operator { get; set; } = "EQ";
    }
}

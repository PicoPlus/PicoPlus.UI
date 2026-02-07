using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

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

        public Contact(IHttpClientFactory httpClientFactory, IConfiguration configuration, ILogger<Contact> logger)
        {
            _httpClientFactory = httpClientFactory;
            _configuration = configuration;
            _logger = logger;

            // Read from environment variable first, then configuration
            _hubSpotToken = Environment.GetEnvironmentVariable("HUBSPOT_TOKEN")
                            ?? configuration["HubSpot:Token"]
                            ?? throw new InvalidOperationException("HubSpot token is not configured. Set HUBSPOT_TOKEN environment variable or HubSpot:Token in appsettings.");
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

            // Build payload using latest HubSpot API v3 format
            var payload = new
            {

                limit = limit,
                after = after,
                sorts = sorts ?? Array.Empty<string>(), // String array instead of object array
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

            var options = new JsonSerializerOptions
            {
                DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
            };

            var json = JsonSerializer.Serialize(payload, options);
            var request = new HttpRequestMessage(HttpMethod.Post, url)
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json")
            };
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _hubSpotToken);

            using var response = await httpClient.SendAsync(request);
            if (!response.IsSuccessStatusCode)
            {
                await ThrowHubSpotRequestExceptionAsync(response, "Search");
            }

            var responseJson = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<Models.CRM.Objects.Contact.Search.Response>(responseJson)!;
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

            // Build filter groups
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

            var options = new JsonSerializerOptions
            {
                DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
            };

            var json = JsonSerializer.Serialize(payload, options);
            var request = new HttpRequestMessage(HttpMethod.Post, url)
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json")
            };
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _hubSpotToken);

            using var response = await httpClient.SendAsync(request);
            if (!response.IsSuccessStatusCode)
            {
                await ThrowHubSpotRequestExceptionAsync(response, "SearchAdvanced");
            }

            var responseJson = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<Models.CRM.Objects.Contact.Search.Response>(responseJson)!;
        }

        private async Task ThrowHubSpotRequestExceptionAsync(HttpResponseMessage response, string operation)
        {
            var responseBody = await response.Content.ReadAsStringAsync();

            _logger.LogError(
                "HubSpot contact operation {Operation} failed. Status: {StatusCode}, Body: {ResponseBody}",
                operation,
                response.StatusCode,
                responseBody);

            var message = response.StatusCode switch
            {
                System.Net.HttpStatusCode.Unauthorized =>
                    "احراز هویت HubSpot نامعتبر است. لطفاً توکن را بررسی کنید.",
                System.Net.HttpStatusCode.Forbidden =>
                    "دسترسی HubSpot رد شد (403). احتمالاً دسترسی لازم برای مخاطبین فعال نیست یا توکن صحیح نیست.",
                System.Net.HttpStatusCode.TooManyRequests =>
                    "تعداد درخواست‌ها به HubSpot بیش از حد مجاز است. لطفاً کمی بعد دوباره تلاش کنید.",
                _ =>
                    $"خطا در ارتباط با HubSpot ({(int)response.StatusCode})."
            };

            throw new HttpRequestException(message, null, response.StatusCode);
        }

        /// <summary>
        /// Create a new contact
        /// POST /crm/v3/objects/contacts
        /// </summary>
        public async Task<Models.CRM.Objects.Contact.Create.Response> Create(
            Models.CRM.Objects.Contact.Create.Request contactInfo)
        {
            var httpClient = _httpClientFactory.CreateClient("HubSpot");
            var json = JsonSerializer.Serialize(contactInfo);
            var request = new HttpRequestMessage(HttpMethod.Post, BaseUrl)
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json")
            };
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _hubSpotToken);

            using var response = await httpClient.SendAsync(request);
            response.EnsureSuccessStatusCode();

            var responseJson = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<Models.CRM.Objects.Contact.Create.Response>(responseJson)!;
        }

        /// <summary>
        /// Get contact by ID
        /// GET /crm/v3/objects/contacts/{contactId}
        /// </summary>
        public async Task<Models.CRM.Objects.Contact.Read.Response> Read(string id, string[]? properties = null, string[]? associations = null)
        {
            var httpClient = _httpClientFactory.CreateClient("HubSpot");
            var url = $"{BaseUrl}/{id}";
            var queryParams = new List<string>();

            if (properties != null && properties.Length > 0)
            {
                foreach (var prop in properties)
                {
                    queryParams.Add($"properties={prop}");
                }
            }
            else
            {
                // Default properties
                queryParams.Add("properties=father_name");
                queryParams.Add("properties=dateofbirth");
                queryParams.Add("properties=natcode");
                queryParams.Add("properties=shahkar_status");
                queryParams.Add("properties=wallet");
                queryParams.Add("properties=total_revenue");
                queryParams.Add("properties=firstname");
                queryParams.Add("properties=lastname");
                queryParams.Add("properties=phone");
                queryParams.Add("properties=gender");
                queryParams.Add("properties=last_products_bought_product_1_image_url");

                queryParams.Add("properties=email");
            }

            if (associations != null && associations.Length > 0)
            {
                foreach (var assoc in associations)
                {
                    queryParams.Add($"associations={assoc}");
                }
            }
            else
            {
                queryParams.Add("associations=deals");
            }

            url += "?" + string.Join("&", queryParams);

            var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _hubSpotToken);

            using var response = await httpClient.SendAsync(request);
            response.EnsureSuccessStatusCode();

            var responseJson = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<Models.CRM.Objects.Contact.Read.Response>(responseJson)!;
        }

        /// <summary>
        /// Update specific contact properties
        /// PATCH /crm/v3/objects/contacts/{contactId}
        /// </summary>
        public async Task UpdateContactProperties(string contactId, Dictionary<string, string> properties)
        {
            var httpClient = _httpClientFactory.CreateClient("HubSpot");
            var url = $"{BaseUrl}/{contactId}";

            var payload = new { properties = properties };
            var json = JsonSerializer.Serialize(payload);
            var request = new HttpRequestMessage(HttpMethod.Patch, url)
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json")
            };
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _hubSpotToken);

            using var response = await httpClient.SendAsync(request);
            response.EnsureSuccessStatusCode();
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

            var json = JsonSerializer.Serialize(new { properties = updatedProperties });
            var request = new HttpRequestMessage(HttpMethod.Patch, url)
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json")
            };
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _hubSpotToken);

            using var response = await httpClient.SendAsync(request);
            response.EnsureSuccessStatusCode();

            var responseJson = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<Models.CRM.Objects.Contact.Search.Response.Result.Properties>(responseJson);
        }

        /// <summary>
        /// Delete a contact (archive)
        /// DELETE /crm/v3/objects/contacts/{contactId}
        /// </summary>
        public async Task<bool> Delete(string contactId)
        {
            var httpClient = _httpClientFactory.CreateClient("HubSpot");
            var url = $"{BaseUrl}/{contactId}";

            var request = new HttpRequestMessage(HttpMethod.Delete, url);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _hubSpotToken);

            using var response = await httpClient.SendAsync(request);
            return response.IsSuccessStatusCode;
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
            var queryParams = new List<string> { $"limit={limit}" };

            if (!string.IsNullOrEmpty(after))
            {
                queryParams.Add($"after={after}");
            }

            if (properties != null && properties.Length > 0)
            {
                foreach (var prop in properties)
                {
                    queryParams.Add($"properties={prop}");
                }
            }

            var url = $"{BaseUrl}?{string.Join("&", queryParams)}";

            var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _hubSpotToken);

            using var response = await httpClient.SendAsync(request);
            response.EnsureSuccessStatusCode();

            var responseJson = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<Models.CRM.Objects.Contact.Search.Response>(responseJson);
        }

        /// <summary>
        /// Batch create contacts
        /// POST /crm/v3/objects/contacts/batch/create
        /// </summary>
        public async Task<dynamic> BatchCreate(List<Models.CRM.Objects.Contact.Create.Request> contacts)
        {
            var httpClient = _httpClientFactory.CreateClient("HubSpot");
            var url = $"{BaseUrl}/batch/create";

            var payload = new { inputs = contacts };
            var json = JsonSerializer.Serialize(payload);
            var request = new HttpRequestMessage(HttpMethod.Post, url)
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json")
            };
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _hubSpotToken);

            using var response = await httpClient.SendAsync(request);
            response.EnsureSuccessStatusCode();

            var responseJson = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<dynamic>(responseJson);
        }

        /// <summary>
        /// Batch update contacts
        /// POST /crm/v3/objects/contacts/batch/update
        /// </summary>
        public async Task<dynamic> BatchUpdate(List<object> updates)
        {
            var httpClient = _httpClientFactory.CreateClient("HubSpot");
            var url = $"{BaseUrl}/batch/update";

            var payload = new { inputs = updates };
            var json = JsonSerializer.Serialize(payload);
            var request = new HttpRequestMessage(HttpMethod.Post, url)
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json")
            };
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _hubSpotToken);

            using var response = await httpClient.SendAsync(request);
            response.EnsureSuccessStatusCode();

            var responseJson = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<dynamic>(responseJson)!;
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

                // Step 2: Update contact property with file URL
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

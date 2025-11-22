using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace PicoPlus.Services.Identity
{
    #region Custom Exception

    public class ZibalApiException : Exception
    {
        public int ResultCode { get; }
        public string FaMessage { get; }

        public ZibalApiException(int resultCode, string message, string faMessage) 
            : base($"Zibal Error [{resultCode}]: {message} | {faMessage}")
        {
            ResultCode = resultCode;
            FaMessage = faMessage;
        }
    }

    #endregion

    public class Zibal
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;
        private readonly ILogger<Zibal> _logger;
        private readonly string _token;
        private const string BaseUrl = "https://api.zibal.ir/v1/facility";

        public Zibal(HttpClient httpClient, IConfiguration configuration, ILogger<Zibal> logger)
        {
            _httpClient = httpClient;
            _configuration = configuration;
            _logger = logger;

            _token = Environment.GetEnvironmentVariable("ZIBAL_TOKEN")
                     ?? configuration["Zibal:Token"]
                     ?? throw new InvalidOperationException("Zibal token is not configured.");
        }

        #region Private Helper Methods

        private async Task<TResponse> SendRequestAsync<TRequest, TResponse>(string endpoint, TRequest requestDto)
        {
            string resultString = string.Empty;
            try
            {
                var requestUrl = $"{BaseUrl}/{endpoint}";
                var json = JsonConvert.SerializeObject(requestDto);
                var httpContent = new StringContent(json, Encoding.UTF8, "application/json");

                var requestMessage = new HttpRequestMessage(HttpMethod.Post, requestUrl);
                requestMessage.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _token);
                requestMessage.Content = httpContent;

                _logger.LogDebug("🚀 Sending request to Zibal: {Endpoint}", endpoint);

                var response = await _httpClient.SendAsync(requestMessage);
                resultString = await response.Content.ReadAsStringAsync();

                _logger.LogDebug("📩 Zibal Response ({StatusCode}): {Response}", response.StatusCode, resultString);

                // --- FIX FOR CS8600 & CS8604 Start ---
                int zibalResultCode = -1;
                string zibalMessage = "Unknown Error";
                bool isJsonParsed = false;

                try
                {
                    var jObject = JObject.Parse(resultString);
                    
                    // اصلاح: چک کردن نال بودن توکن قبل از کست کردن به int
                    var resultToken = jObject["result"];
                    if (resultToken != null && resultToken.Type != JTokenType.Null)
                    {
                        zibalResultCode = (int)resultToken;
                        isJsonParsed = true;
                    }

                    // اصلاح: استفاده از کست نال‌پذیر و مقدار پیش‌فرض
                    zibalMessage = (string?)jObject["message"] ?? "Unknown Error";
                }
                catch
                {
                    // Parsing failed
                }
                // --- FIX End ---

                if (!isJsonParsed && !response.IsSuccessStatusCode)
                {
                    throw new HttpRequestException($"Zibal HTTP Error {response.StatusCode}: {resultString}");
                }

                if (isJsonParsed && zibalResultCode != 1)
                {
                    var persianError = GetPersianErrorMessage(zibalResultCode);
                    _logger.LogWarning("⚠️ Zibal Logic Error: Code={Code}, Message={FaMessage}", zibalResultCode, persianError);
                    throw new ZibalApiException(zibalResultCode, zibalMessage, persianError);
                }

                response.EnsureSuccessStatusCode();

                var result = JsonConvert.DeserializeObject<TResponse>(resultString);

                // اصلاح: هندل کردن وضعیت نال بودن خروجی Deserialize (CS8603)
                if (result == null)
                {
                    throw new Exception("Zibal response was null or could not be deserialized.");
                }

                _logger.LogDebug("✅ Zibal request successful: {Endpoint}", endpoint);
                return result;
            }
            catch (ZibalApiException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "🔥 Critical Error calling Zibal endpoint: {Endpoint}", endpoint);
                throw;
            }
        }

        private string GetPersianErrorMessage(int resultCode)
        {
            return resultCode switch
            {
                1 => "موفق",
                2 => "API Key به درستی ارسال نشده است.",
                3 => "API Key صحیح نیست.",
                4 => "اجازه دسترسی به این سرویس صادر نشده‌است.",
                5 => "callbackUrl نامعتبر است.",
                6 => "مقدار ورودی نامعتبر است.",
                7 => "IP ارسال‌کننده درخواست نامعتبر می‌باشد.",
                8 => "API Key غیرفعال است.",
                9 => "حداقل مبلغ باید 1000 ریال باشد.",
                21 => "شماره شبای وارد شده معتبر نیست.",
                29 => "موجودی کیف‌پول کارمزد کافی نیست.",
                44 => "با ورودی‌های داده شده، شبای مورد نظر یافت نشد.",
                45 => "سرویس‌دهنده‌ها در دسترس نیستند.",
                _ => "خطای ناشناخته از سمت سرویس دهنده"
            };
        }

        #endregion

        #region Public Methods

        public async Task<Models.Services.Identity.Zibal.NationalIdentityInquiry.Response> NationalIdentityInquiryAsync(
            Models.Services.Identity.Zibal.NationalIdentityInquiry.Request requestDto)
        {
            return await SendRequestAsync<
                Models.Services.Identity.Zibal.NationalIdentityInquiry.Request,
                Models.Services.Identity.Zibal.NationalIdentityInquiry.Response>(
                    "nationalIdentityInquiry", requestDto);
        }
        
        // Backward compatibility
        public async Task<Models.Services.Identity.Zibal.NationalIdentityInquiry.Response> NationalIdentityInquiry(
           Models.Services.Identity.Zibal.NationalIdentityInquiry.Request requestDto) => 
           await NationalIdentityInquiryAsync(requestDto);


        public async Task<Models.Services.Identity.Zibal.ShahkarInquiry.Response> ShahkarInquiryAsync(
            Models.Services.Identity.Zibal.ShahkarInquiry.Request requestDto)
        {
            return await SendRequestAsync<
                Models.Services.Identity.Zibal.ShahkarInquiry.Request,
                Models.Services.Identity.Zibal.ShahkarInquiry.Response>(
                    "shahkarInquiry", requestDto);
        }
        
         public async Task<Models.Services.Identity.Zibal.ShahkarInquiry.Response> ShahkerInquiry(
            Models.Services.Identity.Zibal.ShahkarInquiry.Request requestDto) => 
            await ShahkarInquiryAsync(requestDto);

        public async Task<Models.Services.Identity.Zibal.PostalCodeInquiry.Response> PostalCodeInquiryAsync(
            Models.Services.Identity.Zibal.PostalCodeInquiry.Request requestDto)
        {
            return await SendRequestAsync<
                Models.Services.Identity.Zibal.PostalCodeInquiry.Request,
                Models.Services.Identity.Zibal.PostalCodeInquiry.Response>(
                    "postalCodeInquiry", requestDto);
        }

        public async Task<string> GetPostalCodeAddressAsync(string postalCode)
        {
            try
            {
                var request = new Models.Services.Identity.Zibal.PostalCodeInquiry.Request { postalCode = postalCode };
                var response = await PostalCodeInquiryAsync(request);

                // اصلاح برای جلوگیری از نال رفرنس در زنجیره دسترسی‌ها
                if (response?.data?.address != null)
                {
                    var addr = response.data.address;
                    return $"{addr.province}, {addr.town}, {addr.district}, {addr.street}, {addr.street2}, پلاک {addr.number}, طبقه {addr.floor}, واحد {addr.sideFloor}, نام ساختمان {addr.buildingName}";
                }

                return response?.message ?? "اطلاعات یافت نشد";
            }
            catch (ZibalApiException ex)
            {
                return $"خطا: {ex.FaMessage}";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting postal code address: {PostalCode}", postalCode);
                return "خطا در دریافت اطلاعات";
            }
        }
        
        public async Task<string> GetPostalCode(string zipCode) => await GetPostalCodeAddressAsync(zipCode);

        public async Task<Models.Services.Identity.Zibal.IbanInquiry.Response> IbanInquiryAsync(
            Models.Services.Identity.Zibal.IbanInquiry.Request requestDto)
        {
            return await SendRequestAsync<
                Models.Services.Identity.Zibal.IbanInquiry.Request,
                Models.Services.Identity.Zibal.IbanInquiry.Response>(
                    "ibanInquiry", requestDto);
        }

        public async Task<Models.Services.Identity.Zibal.BankAccountInquiry.Response> BankAccountInquiryAsync(
            Models.Services.Identity.Zibal.BankAccountInquiry.Request requestDto)
        {
            return await SendRequestAsync<
                Models.Services.Identity.Zibal.BankAccountInquiry.Request,
                Models.Services.Identity.Zibal.BankAccountInquiry.Response>(
                    "bankAccountInquiry", requestDto);
        }

        public async Task<Models.Services.Identity.Zibal.CardNumberInquiry.Response> CardNumberInquiryAsync(
            Models.Services.Identity.Zibal.CardNumberInquiry.Request requestDto)
        {
            return await SendRequestAsync<
                Models.Services.Identity.Zibal.CardNumberInquiry.Request,
                Models.Services.Identity.Zibal.CardNumberInquiry.Response>(
                    "cardNumberInquiry", requestDto);
        }

        public async Task<Models.Services.Identity.Zibal.LicensePlateInquiry.Response> LicensePlateInquiryAsync(
            Models.Services.Identity.Zibal.LicensePlateInquiry.Request requestDto)
        {
            return await SendRequestAsync<
                Models.Services.Identity.Zibal.LicensePlateInquiry.Request,
                Models.Services.Identity.Zibal.LicensePlateInquiry.Response>(
                    "licensePlateInquiry", requestDto);
        }

        public async Task<Models.Services.Identity.Zibal.BirthCertificateInquiry.Response> BirthCertificateInquiryAsync(
            Models.Services.Identity.Zibal.BirthCertificateInquiry.Request requestDto)
        {
            return await SendRequestAsync<
                Models.Services.Identity.Zibal.BirthCertificateInquiry.Request,
                Models.Services.Identity.Zibal.BirthCertificateInquiry.Response>(
                    "birthCertificateInquiry", requestDto);
        }

        public async Task<Models.Services.Identity.Zibal.PhoneNumberInquiry.Response> PhoneNumberInquiryAsync(
            Models.Services.Identity.Zibal.PhoneNumberInquiry.Request requestDto)
        {
            return await SendRequestAsync<
                Models.Services.Identity.Zibal.PhoneNumberInquiry.Request,
                Models.Services.Identity.Zibal.PhoneNumberInquiry.Response>(
                    "phoneNumberInquiry", requestDto);
        }

        public async Task<Models.Services.Identity.Zibal.CompanyInquiry.Response> CompanyInquiryAsync(
            Models.Services.Identity.Zibal.CompanyInquiry.Request requestDto)
        {
            return await SendRequestAsync<
                Models.Services.Identity.Zibal.CompanyInquiry.Request,
                Models.Services.Identity.Zibal.CompanyInquiry.Response>(
                    "companyInquiry", requestDto);
        }

        public async Task<Models.Services.Identity.Zibal.PassportInquiry.Response> PassportInquiryAsync(
            Models.Services.Identity.Zibal.PassportInquiry.Request requestDto)
        {
            return await SendRequestAsync<
                Models.Services.Identity.Zibal.PassportInquiry.Request,
                Models.Services.Identity.Zibal.PassportInquiry.Response>(
                    "passportInquiry", requestDto);
        }

        public async Task<Models.Services.Identity.Zibal.NationalCardImageInquiry.Response> NationalCardImageInquiryAsync(
            Models.Services.Identity.Zibal.NationalCardImageInquiry.Request requestDto)
        {
            return await SendRequestAsync<
                Models.Services.Identity.Zibal.NationalCardImageInquiry.Request,
                Models.Services.Identity.Zibal.NationalCardImageInquiry.Response>(
                    "nationalCardImageInquiry", requestDto);
        }

        #endregion
    }
}

using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq; // حتما این رو اضافه کن برای پارس کردن اولیه

namespace PicoPlus.Services.Identity
{
    /// <summary>
    /// Zibal Inquiry Services - Complete implementation with Advanced Error Handling
    /// </summary>
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

            // دریافت توکن با اولویت متغیر محیطی
            _token = Environment.GetEnvironmentVariable("ZIBAL_TOKEN")
                     ?? configuration["Zibal:Token"]
                     ?? throw new InvalidOperationException("Zibal token is missing! کجاست این توکن بینوایان؟");
        }

        #region Private Helper Methods (The Brain 🧠)

        /// <summary>
        /// متد هوشمند ارسال درخواست که خطاها را طبق مستندات زیبال ترجمه می‌کند
        /// </summary>
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

                // لاگ کردن وضعیت
                _logger.LogDebug("📩 Zibal Response ({StatusCode}): {Response}", response.StatusCode, resultString);

                // مرحله ۱: بررسی خطاهای عمومی HTTP (مثل 401, 500 و...)
                // طبق مستندات: 400, 403, 500 یعنی خطا. اما ما باید بادی رو بخونیم تا بفهمیم کدوم کد خطاست.
                
                // تلاش می‌کنیم بفهمیم کد result چی بوده
                int zibalResultCode = -1;
                string zibalMessage = "Unknown Error";

                try
                {
                    var jObject = JObject.Parse(resultString);
                    if (jObject["result"] != null)
                    {
                        zibalResultCode = (int)jObject["result"];
                    }
                    if (jObject["message"] != null)
                    {
                        zibalMessage = (string)jObject["message"];
                    }
                }
                catch
                {
                    // اگر جیسون نبود یا فرمت عجیب بود و ریسپانس کد هم اوکی نبود
                    if (!response.IsSuccessStatusCode)
                    {
                         throw new HttpRequestException($"HTTP Error {response.StatusCode}: {resultString}");
                    }
                }

                // مرحله ۲: بررسی کد result طبق جدول درخواستی
                if (zibalResultCode != 1) // 1 یعنی موفقیت
                {
                    var persianError = GetPersianErrorMessage(zibalResultCode);
                    
                    _logger.LogWarning("⚠️ Zibal Business Error: Code={Code}, Message={FaMessage}", zibalResultCode, persianError);
                    
                    // پرتاب اکسپشن اختصاصی که کنترلر شما بتونه اون رو بگیره و به کاربر نشون بده
                    throw new ZibalApiException(zibalResultCode, zibalMessage, persianError);
                }

                // مرحله ۳: اگر همه چیز گل و بلبل بود (result == 1)
                var result = JsonConvert.DeserializeObject<TResponse>(resultString);
                return result;
            }
            catch (ZibalApiException)
            {
                throw; // اینو که خودمون ساختیم، ردش کن بره بالا
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "🔥 Critical Error calling Zibal endpoint: {Endpoint}", endpoint);
                throw;
            }
        }

        /// <summary>
        /// مترجم کدهای خطای زیبال به زبان شیرین فارسی
        /// </summary>
        private string GetPersianErrorMessage(int resultCode)
        {
            return resultCode switch
            {
                1 => "عملیات با موفقیت انجام شد",
                2 => "API Key به درستی ارسال نشده است",
                3 => "API Key صحیح نیست",
                4 => "اجازه دسترسی به این سرویس صادر نشده‌است (مجوز نداری داداش!)",
                5 => "آدرس بازگشت (callbackUrl) نامعتبر است",
                6 => "مقدار ورودی نامعتبر است (دیتای پرت فرستادی)",
                7 => "IP ارسال‌کننده درخواست نامعتبر می‌باشد",
                8 => "API Key غیرفعال است",
                9 => "حداقل مبلغ باید 1000 ریال باشد (پول خرد قبول نمیکنیم)",
                21 => "شماره شبای وارد شده معتبر نیست (26 کاراکتر، شروع با IR، بدون خط تیره و فاصله)",
                29 => "موجودی کیف‌پول کارمزد برای این عملیات کافی نیست (کفگیر خورده ته دیگ!)",
                44 => "با ورودی‌های داده شده شبای مورد نظر یافت نشد",
                45 => "سرویس‌دهنده‌ها برای استعلام در دسترس نیستند (سیستم قطعه، بعدا تماس بگیرید)",
                _ => "خطای ناشناخته از سمت سرویس دهنده"
            };
        }

        #endregion

        #region National Identity Inquiry (استعلام هویت)

        public async Task<Models.Services.Identity.Zibal.NationalIdentityInquiry.Response> NationalIdentityInquiryAsync(
            Models.Services.Identity.Zibal.NationalIdentityInquiry.Request requestDto)
        {
            return await SendRequestAsync<
                Models.Services.Identity.Zibal.NationalIdentityInquiry.Request,
                Models.Services.Identity.Zibal.NationalIdentityInquiry.Response>(
                    "nationalIdentityInquiry", requestDto);
        }

        public async Task<Models.Services.Identity.Zibal.NationalIdentityInquiry.Response> NationalIdentityInquiry(
            Models.Services.Identity.Zibal.NationalIdentityInquiry.Request requestDto) => await NationalIdentityInquiryAsync(requestDto);

        #endregion

        #region Shahkar Inquiry (استعلام شاهکار)

        public async Task<Models.Services.Identity.Zibal.ShahkarInquiry.Response> ShahkarInquiryAsync(
            Models.Services.Identity.Zibal.ShahkarInquiry.Request requestDto)
        {
            return await SendRequestAsync<
                Models.Services.Identity.Zibal.ShahkarInquiry.Request,
                Models.Services.Identity.Zibal.ShahkarInquiry.Response>(
                    "shahkarInquiry", requestDto);
        }

        public async Task<Models.Services.Identity.Zibal.ShahkarInquiry.Response> ShahkerInquiry(
            Models.Services.Identity.Zibal.ShahkarInquiry.Request requestDto) => await ShahkarInquiryAsync(requestDto);

        #endregion

        #region Postal Code Inquiry (استعلام کد پستی)

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

                if (response?.data?.address != null)
                {
                    var addr = response.data.address;
                    return $"{addr.province}, {addr.town}, {addr.district}, {addr.street}, {addr.street2}, پلاک {addr.number}, طبقه {addr.floor}, واحد {addr.sideFloor}, نام ساختمان {addr.buildingName}";
                }
                return "اطلاعات یافت نشد";
            }
            catch (ZibalApiException ex)
            {
                return $"خطا: {ex.FaMessage}";
            }
            catch
            {
                return "خطا در دریافت اطلاعات";
            }
        }

        public async Task<string> GetPostalCode(string zipCode) => await GetPostalCodeAddressAsync(zipCode);

        #endregion

        #region IBAN Inquiry (استعلام شبا)

        public async Task<Models.Services.Identity.Zibal.IbanInquiry.Response> IbanInquiryAsync(
            Models.Services.Identity.Zibal.IbanInquiry.Request requestDto)
        {
            return await SendRequestAsync<
                Models.Services.Identity.Zibal.IbanInquiry.Request,
                Models.Services.Identity.Zibal.IbanInquiry.Response>(
                    "ibanInquiry", requestDto);
        }

        #endregion

        #region Bank Account Inquiry (استعلام حساب بانکی)

        public async Task<Models.Services.Identity.Zibal.BankAccountInquiry.Response> BankAccountInquiryAsync(
            Models.Services.Identity.Zibal.BankAccountInquiry.Request requestDto)
        {
            return await SendRequestAsync<
                Models.Services.Identity.Zibal.BankAccountInquiry.Request,
                Models.Services.Identity.Zibal.BankAccountInquiry.Response>(
                    "bankAccountInquiry", requestDto);
        }

        #endregion

        #region Card Number Inquiry (استعلام شماره کارت)

        public async Task<Models.Services.Identity.Zibal.CardNumberInquiry.Response> CardNumberInquiryAsync(
            Models.Services.Identity.Zibal.CardNumberInquiry.Request requestDto)
        {
            return await SendRequestAsync<
                Models.Services.Identity.Zibal.CardNumberInquiry.Request,
                Models.Services.Identity.Zibal.CardNumberInquiry.Response>(
                    "cardNumberInquiry", requestDto);
        }

        #endregion

        #region License Plate Inquiry (استعلام پلاک خودرو)

        public async Task<Models.Services.Identity.Zibal.LicensePlateInquiry.Response> LicensePlateInquiryAsync(
            Models.Services.Identity.Zibal.LicensePlateInquiry.Request requestDto)
        {
            return await SendRequestAsync<
                Models.Services.Identity.Zibal.LicensePlateInquiry.Request,
                Models.Services.Identity.Zibal.LicensePlateInquiry.Response>(
                    "licensePlateInquiry", requestDto);
        }

        #endregion

        #region Birth Certificate Inquiry (استعلام شناسنامه)

        public async Task<Models.Services.Identity.Zibal.BirthCertificateInquiry.Response> BirthCertificateInquiryAsync(
            Models.Services.Identity.Zibal.BirthCertificateInquiry.Request requestDto)
        {
            return await SendRequestAsync<
                Models.Services.Identity.Zibal.BirthCertificateInquiry.Request,
                Models.Services.Identity.Zibal.BirthCertificateInquiry.Response>(
                    "birthCertificateInquiry", requestDto);
        }

        #endregion

        #region Phone Number Inquiry (استعلام تلفن ثابت)

        public async Task<Models.Services.Identity.Zibal.PhoneNumberInquiry.Response> PhoneNumberInquiryAsync(
            Models.Services.Identity.Zibal.PhoneNumberInquiry.Request requestDto)
        {
            return await SendRequestAsync<
                Models.Services.Identity.Zibal.PhoneNumberInquiry.Request,
                Models.Services.Identity.Zibal.PhoneNumberInquiry.Response>(
                    "phoneNumberInquiry", requestDto);
        }

        #endregion

        #region Company Inquiry (استعلام شرکت)

        public async Task<Models.Services.Identity.Zibal.CompanyInquiry.Response> CompanyInquiryAsync(
            Models.Services.Identity.Zibal.CompanyInquiry.Request requestDto)
        {
            return await SendRequestAsync<
                Models.Services.Identity.Zibal.CompanyInquiry.Request,
                Models.Services.Identity.Zibal.CompanyInquiry.Response>(
                    "companyInquiry", requestDto);
        }

        #endregion

        #region Passport Inquiry (استعلام گذرنامه)

        public async Task<Models.Services.Identity.Zibal.PassportInquiry.Response> PassportInquiryAsync(
            Models.Services.Identity.Zibal.PassportInquiry.Request requestDto)
        {
            return await SendRequestAsync<
                Models.Services.Identity.Zibal.PassportInquiry.Request,
                Models.Services.Identity.Zibal.PassportInquiry.Response>(
                    "passportInquiry", requestDto);
        }

        #endregion

        #region National Card Image Inquiry (دریافت تصویر کارت ملی)

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

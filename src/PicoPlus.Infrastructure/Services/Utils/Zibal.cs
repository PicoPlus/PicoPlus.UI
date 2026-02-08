using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace PicoPlus.Services.Identity
{
    /// <summary>
    /// Zibal Inquiry Services - Complete implementation of all Zibal inquiry endpoints
    /// Documentation: https://help.zibal.ir/facilities
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

            // Read from environment variable first, then configuration
            _token = Environment.GetEnvironmentVariable("ZIBAL_TOKEN")
                     ?? configuration["Zibal:Token"]
                     ?? throw new InvalidOperationException("Zibal token is not configured. Set ZIBAL_TOKEN environment variable or Zibal:Token in appsettings.");
        }

        #region Private Helper Methods

        /// <summary>
        /// Generic method to send POST requests to Zibal API
        /// </summary>
        private async Task<TResponse> SendRequestAsync<TRequest, TResponse>(string endpoint, TRequest requestDto)
        {
            try
            {
                var requestUrl = $"{BaseUrl}/{endpoint}";
                var json = JsonConvert.SerializeObject(requestDto);
                var httpContent = new StringContent(json, Encoding.UTF8, "application/json");

                var requestMessage = new HttpRequestMessage(HttpMethod.Post, requestUrl);
                requestMessage.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _token);
                requestMessage.Content = httpContent;

                _logger.LogDebug("Sending request to Zibal: {Endpoint}", endpoint);

                var response = await _httpClient.SendAsync(requestMessage);
                var resultString = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning("Zibal request failed: {Endpoint}, Status: {Status}, Response: {Response}",
                        endpoint, response.StatusCode, resultString);
                }
                else
                {
                    _logger.LogDebug("Zibal API response for {Endpoint}: {Response}", endpoint, resultString);
                }

                response.EnsureSuccessStatusCode();

                var result = JsonConvert.DeserializeObject<TResponse>(resultString);
                _logger.LogDebug("Zibal request successful: {Endpoint}", endpoint);

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calling Zibal endpoint: {Endpoint}", endpoint);
                throw;
            }
        }

        #endregion

        #region National Identity Inquiry (استعلام هویت)

        /// <summary>
        /// استعلام هویت - تایید کد ملی با تاریخ تولد
        /// National Identity Inquiry - Verify Iranian national ID with birth date
        /// </summary>
        /// <param name="requestDto">Request containing nationalCode and birthDate (Persian format)</param>
        /// <returns>Response with firstName, lastName, fatherName, alive status</returns>
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
            Models.Services.Identity.Zibal.NationalIdentityInquiry.Request requestDto)
        {
            return await NationalIdentityInquiryAsync(requestDto);
        }

        #endregion

        #region Shahkar Inquiry (استعلام شاهکار)

        /// <summary>
        /// استعلام شاهکار - تایید مالکیت شماره موبایل با کد ملی
        /// Shahkar Inquiry - Verify mobile number ownership with national code
        /// </summary>
        /// <param name="requestDto">Request containing mobile and nationalCode</param>
        /// <returns>Response with matched status</returns>
        public async Task<Models.Services.Identity.Zibal.ShahkarInquiry.Response> ShahkarInquiryAsync(
            Models.Services.Identity.Zibal.ShahkarInquiry.Request requestDto)
        {
            return await SendRequestAsync<
                Models.Services.Identity.Zibal.ShahkarInquiry.Request,
                Models.Services.Identity.Zibal.ShahkarInquiry.Response>(
                    "shahkarInquiry", requestDto);
        }

        // Backward compatibility (fix typo)
        public async Task<Models.Services.Identity.Zibal.ShahkarInquiry.Response> ShahkerInquiry(
            Models.Services.Identity.Zibal.ShahkarInquiry.Request requestDto)
        {
            return await ShahkarInquiryAsync(requestDto);
        }

        #endregion

        #region Postal Code Inquiry (استعلام کد پستی)

        /// <summary>
        /// استعلام کد پستی - دریافت اطلاعات آدرس از کد پستی
        /// Postal Code Inquiry - Get full address details from postal code
        /// </summary>
        /// <param name="requestDto">Request containing 10-digit postal code</param>
        /// <returns>Response with complete address information</returns>
        public async Task<Models.Services.Identity.Zibal.PostalCodeInquiry.Response> PostalCodeInquiryAsync(
            Models.Services.Identity.Zibal.PostalCodeInquiry.Request requestDto)
        {
            return await SendRequestAsync<
                Models.Services.Identity.Zibal.PostalCodeInquiry.Request,
                Models.Services.Identity.Zibal.PostalCodeInquiry.Response>(
                    "postalCodeInquiry", requestDto);
        }

        /// <summary>
        /// Get formatted address string from postal code
        /// </summary>
        public async Task<string> GetPostalCodeAddressAsync(string postalCode)
        {
            try
            {
                var request = new Models.Services.Identity.Zibal.PostalCodeInquiry.Request
                {
                    postalCode = postalCode
                };

                var response = await PostalCodeInquiryAsync(request);

                if (response?.data?.address != null)
                {
                    var addr = response.data.address;
                    return $"{addr.province}, {addr.town}, {addr.district}, " +
                           $"{addr.street}, {addr.street2}, پلاک {addr.number}, " +
                           $"طبقه {addr.floor}, واحد {addr.sideFloor}, نام ساختمان {addr.buildingName}";
                }

                return response?.message ?? "اطلاعات یافت نشد";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting postal code address: {PostalCode}", postalCode);
                return "خطا در دریافت اطلاعات";
            }
        }

        // Backward compatibility
        public async Task<string> GetPostalCode(string zipCode)
        {
            return await GetPostalCodeAddressAsync(zipCode);
        }

        #endregion

        #region IBAN Inquiry (استعلام شبا)

        /// <summary>
        /// استعلام شماره شبا - تایید شماره حساب شبا
        /// IBAN Inquiry - Verify Iranian IBAN (Sheba) account number
        /// </summary>
        /// <param name="requestDto">Request containing 26-character IBAN starting with IR</param>
        /// <returns>Response with account status, bank name, and owner name</returns>
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

        /// <summary>
        /// استعلام حساب بانکی - تایید حساب بانکی با کد ملی
        /// Bank Account Inquiry - Verify bank account with national code
        /// </summary>
        /// <param name="requestDto">Request containing nationalCode, accountNumber, and bankCode</param>
        /// <returns>Response with matched status and account information</returns>
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

        /// <summary>
        /// استعلام شماره کارت - تایید شماره کارت با کد ملی
        /// Card Number Inquiry - Verify 16-digit card number with national code
        /// </summary>
        /// <param name="requestDto">Request containing nationalCode and 16-digit cardNumber</param>
        /// <returns>Response with matched status and card information</returns>
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

        /// <summary>
        /// استعلام پلاک خودرو - دریافت اطلاعات خودرو از شماره پلاک
        /// License Plate Inquiry - Get vehicle information by license plate
        /// </summary>
        /// <param name="requestDto">Request containing plate parts (plateChar1-4)</param>
        /// <returns>Response with vehicle model, color, year, type, and owner information</returns>
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

        /// <summary>
        /// استعلام شناسنامه - تایید اطلاعات شناسنامه
        /// Birth Certificate Inquiry - Verify birth certificate information
        /// </summary>
        /// <param name="requestDto">Request containing certificateNumber, nationalCode, and birthDate</param>
        /// <returns>Response with matched status and personal information</returns>
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

        /// <summary>
        /// استعلام تلفن ثابت - دریافت اطلاعات مشترک از شماره تلفن
        /// Phone Number Inquiry - Get subscriber information by landline phone number
        /// </summary>
        /// <param name="requestDto">Request containing phone number with area code</param>
        /// <returns>Response with subscriber name, address, and postal code</returns>
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

        /// <summary>
        /// استعلام شرکت - دریافت اطلاعات شرکت از شناسه ملی
        /// Company Inquiry - Get company information by national ID or registration number
        /// </summary>
        /// <param name="requestDto">Request containing nationalId or registrationNumber</param>
        /// <returns>Response with company name, type, status, and address</returns>
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

        /// <summary>
        /// استعلام گذرنامه - تایید اطلاعات گذرنامه
        /// Passport Inquiry - Verify passport information
        /// </summary>
        /// <param name="requestDto">Request containing passportNumber and nationalCode</param>
        /// <returns>Response with matched status and passport details</returns>
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

        /// <summary>
        /// دریافت تصویر کارت ملی - Get national card image in Base64 format
        /// </summary>
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

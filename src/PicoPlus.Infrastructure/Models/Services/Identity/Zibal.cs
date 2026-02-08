#nullable enable

namespace PicoPlus.Models.Services.Identity
{
    /// <summary>
    /// Zibal Inquiry API Data Transfer Objects
    /// Documentation: https://help.zibal.ir/facilities
    /// </summary>
    public class Zibal
    {
        #region National Identity Inquiry (استعلام هویت)
        /// <summary>
        /// National Identity Inquiry - Verify Iranian national ID with birth date
        /// Endpoint: /v1/facility/nationalIdentityInquiry
        /// </summary>
        public class NationalIdentityInquiry
        {
            public class Request
            {
                /// <summary>کد ملی 10 رقمی</summary>
                public string? nationalCode { get; set; }

                /// <summary>تاریخ تولد به فرمت شمسی (1370/01/01)</summary>
                public string? birthDate { get; set; }

                public bool genderInquiry { get; set; } = true;
            }

            public class Response
            {
                /// <summary>کد نتیجه: 100 = موفق</summary>
                public int? result { get; set; }

                /// <summary>پیام توضیحی</summary>
                public string? message { get; set; }

                public Data? data { get; set; }

                public class Data
                {
                    /// <summary>آیا اطلاعات مطابقت دارد</summary>
                    public bool? matched { get; set; }

                    /// <summary>نام</summary>
                    public string? firstName { get; set; }

                    /// <summary>نام خانوادگی</summary>
                    public string? lastName { get; set; }

                    /// <summary>نام پدر</summary>
                    public string? fatherName { get; set; }

                    /// <summary>وضعیت حیات</summary>
                    public bool? alive { get; set; }

                    /// <summary>جنسیت: "1" = مرد، "2" = زن</summary>
                    public string? gender { get; set; }
                }
            }
        }
        #endregion

        #region National Card Image Inquiry (دریافت تصویر کارت ملی)
        /// <summary>
        /// National Card Image Inquiry - Get national card image in base64 format
        /// Endpoint: /v1/facility/nationalCardImageInquiry
        /// </summary>
        public class NationalCardImageInquiry
        {
            public class Request
            {
                /// <summary>کد ملی 10 رقمی</summary>
                public string? nationalCode { get; set; }

                /// <summary>تاریخ تولد به فرمت شمسی (1370/01/01)</summary>
                public string? birthDate { get; set; }
            }

            public class Response
            {
                /// <summary>کد نتیجه: 1 = موفق، 0 = ناموفق</summary>
                public int? result { get; set; }

                /// <summary>پیام توضیحی</summary>
                public string? message { get; set; }

                public Data? data { get; set; }

                public class Data
                {
                    /// <summary>آیا اطلاعات مطابقت دارد</summary>
                    public bool? matched { get; set; }

                    /// <summary>تصویر کارت ملی به صورت Base64</summary>
                    public string? nationalCardImage { get; set; }
                }
            }
        }
        #endregion

        #region Shahkar Inquiry (استعلام شاهکار - سامانه احراز هویت)
        /// <summary>
        /// Shahkar Inquiry - Verify mobile number ownership with national code
        /// Endpoint: /v1/facility/shahkarInquiry
        /// </summary>
        public class ShahkarInquiry
        {
            public class Request
            {
                /// <summary>شماره موبایل (09123456789)</summary>
                public string? mobile { get; set; }

                /// <summary>کد ملی 10 رقمی</summary>
                public string? nationalCode { get; set; }
            }

            public class Response
            {
                /// <summary>پیام توضیحی</summary>
                public string? message { get; set; }

                public Data? data { get; set; }

                /// <summary>کد نتیجه: 100 = موفق</summary>
                public int? result { get; set; }

                public class Data
                {
                    /// <summary>آیا شماره موبایل متعلق به کد ملی است</summary>
                    public bool? matched { get; set; }
                }
            }
        }
        #endregion

        #region Postal Code Inquiry (استعلام کد پستی)
        /// <summary>
        /// Postal Code Inquiry - Get full address details from postal code
        /// Endpoint: /v1/facility/postalCodeInquiry
        /// </summary>
        public class PostalCodeInquiry
        {
            public class Request
            {
                /// <summary>کد پستی 10 رقمی</summary>
                public string? postalCode { get; set; }
            }

            public class Response
            {
                /// <summary>کد نتیجه: 100 = موفق</summary>
                public int? result { get; set; }

                /// <summary>پیام توضیحی</summary>
                public string? message { get; set; }

                public Data? data { get; set; }

                public class Data
                {
                    public Address? address { get; set; }
                }

                public class Address
                {
                    /// <summary>استان</summary>
                    public string? province { get; set; }

                    /// <summary>شهر</summary>
                    public string? town { get; set; }

                    /// <summary>منطقه</summary>
                    public string? district { get; set; }

                    /// <summary>خیابان اصلی</summary>
                    public string? street { get; set; }

                    /// <summary>خیابان فرعی</summary>
                    public string? street2 { get; set; }

                    /// <summary>پلاک</summary>
                    public int? number { get; set; }

                    /// <summary>طبقه</summary>
                    public string? floor { get; set; }

                    /// <summary>واحد</summary>
                    public string? sideFloor { get; set; }

                    /// <summary>نام ساختمان</summary>
                    public string? buildingName { get; set; }

                    /// <summary>توضیحات</summary>
                    public string? description { get; set; }
                }
            }
        }
        #endregion

        #region IBAN Inquiry (استعلام شبا)
        /// <summary>
        /// IBAN Inquiry - Verify Iranian IBAN (Sheba) account number
        /// Endpoint: /v1/facility/ibanInquiry
        /// </summary>
        public class IbanInquiry
        {
            public class Request
            {
                /// <summary>شماره شبا (IR بدون فاصله - 26 کاراکتر)</summary>
                public string? iban { get; set; }
            }

            public class Response
            {
                /// <summary>کد نتیجه: 100 = موفق</summary>
                public int? result { get; set; }

                /// <summary>پیام توضیحی</summary>
                public string? message { get; set; }

                public Data? data { get; set; }

                public class Data
                {
                    /// <summary>وضعیت حساب (فعال/غیرفعال)</summary>
                    public string? status { get; set; }

                    /// <summary>نام بانک</summary>
                    public string? bankName { get; set; }

                    /// <summary>نام صاحب حساب</summary>
                    public string? ownerName { get; set; }

                    /// <summary>شماره حساب</summary>
                    public string? accountNumber { get; set; }
                }
            }
        }
        #endregion

        #region Bank Account Inquiry (استعلام حساب بانکی)
        /// <summary>
        /// Bank Account Inquiry - Verify bank account with national code
        /// Endpoint: /v1/facility/bankAccountInquiry
        /// </summary>
        public class BankAccountInquiry
        {
            public class Request
            {
                /// <summary>کد ملی صاحب حساب</summary>
                public string? nationalCode { get; set; }

                /// <summary>شماره حساب بانکی</summary>
                public string? accountNumber { get; set; }

                /// <summary>کد بانک</summary>
                public string? bankCode { get; set; }
            }

            public class Response
            {
                /// <summary>کد نتیجه: 100 = موفق</summary>
                public int? result { get; set; }

                /// <summary>پیام توضیحی</summary>
                public string? message { get; set; }

                public Data? data { get; set; }

                public class Data
                {
                    /// <summary>آیا حساب متعلق به کد ملی است</summary>
                    public bool? matched { get; set; }

                    /// <summary>نام صاحب حساب</summary>
                    public string? ownerName { get; set; }

                    /// <summary>وضعیت حساب</summary>
                    public string? status { get; set; }
                }
            }
        }
        #endregion

        #region Card Number Inquiry (استعلام شماره کارت)
        /// <summary>
        /// Card Number Inquiry - Verify card number with national code
        /// Endpoint: /v1/facility/cardNumberInquiry
        /// </summary>
        public class CardNumberInquiry
        {
            public class Request
            {
                /// <summary>کد ملی صاحب کارت</summary>
                public string? nationalCode { get; set; }

                /// <summary>شماره کارت 16 رقمی</summary>
                public string? cardNumber { get; set; }
            }

            public class Response
            {
                /// <summary>کد نتیجه: 100 = موفق</summary>
                public int? result { get; set; }

                /// <summary>پیام توضیحی</summary>
                public string? message { get; set; }

                public Data? data { get; set; }

                public class Data
                {
                    /// <summary>آیا کارت متعلق به کد ملی است</summary>
                    public bool? matched { get; set; }

                    /// <summary>نام بانک</summary>
                    public string? bankName { get; set; }

                    /// <summary>نوع کارت</summary>
                    public string? cardType { get; set; }
                }
            }
        }
        #endregion

        #region License Plate Inquiry (استعلام پلاک خودرو)
        /// <summary>
        /// License Plate Inquiry - Get vehicle information by license plate
        /// Endpoint: /v1/facility/licensePlateInquiry
        /// </summary>
        public class LicensePlateInquiry
        {
            public class Request
            {
                /// <summary>بخش اول پلاک (دو رقم)</summary>
                public string? plateChar1 { get; set; }

                /// <summary>حرف پلاک (یک حرف فارسی)</summary>
                public string? plateChar2 { get; set; }

                /// <summary>بخش دوم پلاک (سه رقم)</summary>
                public string? plateChar3 { get; set; }

                /// <summary>شماره ایران (دو رقم)</summary>
                public string? plateChar4 { get; set; }
            }

            public class Response
            {
                /// <summary>کد نتیجه: 100 = موفق</summary>
                public int? result { get; set; }

                /// <summary>پیام توضیحی</summary>
                public string? message { get; set; }

                public Data? data { get; set; }

                public class Data
                {
                    /// <summary>مدل خودرو</summary>
                    public string? model { get; set; }

                    /// <summary>رنگ خودرو</summary>
                    public string? color { get; set; }

                    /// <summary>سال ساخت</summary>
                    public string? year { get; set; }

                    /// <summary>نوع خودرو</summary>
                    public string? vehicleType { get; set; }

                    /// <summary>نام مالک</summary>
                    public string? ownerName { get; set; }
                }
            }
        }
        #endregion

        #region Birth Certificate Inquiry (استعلام شناسنامه)
        /// <summary>
        /// Birth Certificate Inquiry - Verify birth certificate information
        /// Endpoint: /v1/facility/birthCertificateInquiry
        /// </summary>
        public class BirthCertificateInquiry
        {
            public class Request
            {
                /// <summary>شماره شناسنامه</summary>
                public string? certificateNumber { get; set; }

                /// <summary>کد ملی</summary>
                public string? nationalCode { get; set; }

                /// <summary>تاریخ تولد (شمسی)</summary>
                public string? birthDate { get; set; }
            }

            public class Response
            {
                /// <summary>کد نتیجه: 100 = موفق</summary>
                public int? result { get; set; }

                /// <summary>پیام توضیحی</summary>
                public string? message { get; set; }

                public Data? data { get; set; }

                public class Data
                {
                    /// <summary>آیا اطلاعات مطابقت دارد</summary>
                    public bool? matched { get; set; }

                    /// <summary>نام</summary>
                    public string? firstName { get; set; }

                    /// <summary>نام خانوادگی</summary>
                    public string? lastName { get; set; }

                    /// <summary>نام پدر</summary>
                    public string? fatherName { get; set; }

                    /// <summary>محل تولد</summary>
                    public string? birthPlace { get; set; }

                    /// <summary>محل صدور</summary>
                    public string? issuePlace { get; set; }
                }
            }
        }
        #endregion

        #region Phone Number Inquiry (استعلام تلفن ثابت)
        /// <summary>
        /// Phone Number Inquiry - Get subscriber information by phone number
        /// Endpoint: /v1/facility/phoneNumberInquiry
        /// </summary>
        public class PhoneNumberInquiry
        {
            public class Request
            {
                /// <summary>شماره تلفن ثابت با کد شهر (02112345678)</summary>
                public string? phoneNumber { get; set; }
            }

            public class Response
            {
                /// <summary>کد نتیجه: 100 = موفق</summary>
                public int? result { get; set; }

                /// <summary>پیام توضیحی</summary>
                public string? message { get; set; }

                public Data? data { get; set; }

                public class Data
                {
                    /// <summary>نام مشترک</summary>
                    public string? subscriberName { get; set; }

                    /// <summary>آدرس</summary>
                    public string? address { get; set; }

                    /// <summary>کد پستی</summary>
                    public string? postalCode { get; set; }
                }
            }
        }
        #endregion

        #region Company Inquiry (استعلام شرکت)
        /// <summary>
        /// Company Inquiry - Get company information by registration number
        /// Endpoint: /v1/facility/companyInquiry
        /// </summary>
        public class CompanyInquiry
        {
            public class Request
            {
                /// <summary>شناسه ملی شرکت</summary>
                public string? nationalId { get; set; }

                /// <summary>شماره ثبت</summary>
                public string? registrationNumber { get; set; }
            }

            public class Response
            {
                /// <summary>کد نتیجه: 100 = موفق</summary>
                public int? result { get; set; }

                /// <summary>پیام توضیحی</summary>
                public string? message { get; set; }

                public Data? data { get; set; }

                public class Data
                {
                    /// <summary>نام شرکت</summary>
                    public string? companyName { get; set; }

                    /// <summary>نوع شرکت</summary>
                    public string? companyType { get; set; }

                    /// <summary>وضعیت شرکت</summary>
                    public string? status { get; set; }

                    /// <summary>تاریخ ثبت</summary>
                    public string? registrationDate { get; set; }

                    /// <summary>آدرس</summary>
                    public string? address { get; set; }

                    /// <summary>کد پستی</summary>
                    public string? postalCode { get; set; }
                }
            }
        }
        #endregion

        #region Passport Inquiry (استعلام گذرنامه)
        /// <summary>
        /// Passport Inquiry - Verify passport information
        /// Endpoint: /v1/facility/passportInquiry
        /// </summary>
        public class PassportInquiry
        {
            public class Request
            {
                /// <summary>شماره گذرنامه</summary>
                public string? passportNumber { get; set; }

                /// <summary>کد ملی</summary>
                public string? nationalCode { get; set; }
            }

            public class Response
            {
                /// <summary>کد نتیجه: 100 = موفق</summary>
                public int? result { get; set; }

                /// <summary>پیام توضیحی</summary>
                public string? message { get; set; }

                public Data? data { get; set; }

                public class Data
                {
                    /// <summary>آیا اطلاعات مطابقت دارد</summary>
                    public bool? matched { get; set; }

                    /// <summary>نام</summary>
                    public string? firstName { get; set; }

                    /// <summary>نام خانوادگی</summary>
                    public string? lastName { get; set; }

                    /// <summary>تاریخ اعتبار</summary>
                    public string? expiryDate { get; set; }

                    /// <summary>تاریخ صدور</summary>
                    public string? issueDate { get; set; }
                }
            }
        }
        #endregion

        // Keep old naming for backward compatibility
        public class GetPostalCode
        {
            public class Request
            {
                public string? postalCode { get; set; }
            }

            public class Response
            {
                public int? result { get; set; }
                public string? message { get; set; }
                public Data? data { get; set; }

                public class Data
                {
                    public Address? address { get; set; }
                }

                public class Address
                {
                    public string? province { get; set; }
                    public string? town { get; set; }
                    public string? district { get; set; }
                    public string? street { get; set; }
                    public string? street2 { get; set; }
                    public int? number { get; set; }
                    public string? floor { get; set; }
                    public string? sideFloor { get; set; }
                    public string? buildingName { get; set; }
                    public string? description { get; set; }
                }
            }
        }
    }
}

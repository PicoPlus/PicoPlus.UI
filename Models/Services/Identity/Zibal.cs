#nullable enable

namespace PicoPlus.Models.Services.Identity
{
    public class Zibal
    {
        public class NationalIdentityInquiry
        {
            public class Request
            {
                public string? nationalCode { get; set; }
                public string? birthDate { get; set; }
            }

            public class Response
            {
                public int? result { get; set; }
                public string? message { get; set; }
                public Data? data { get; set; }

                public class Data
                {
                    public bool? matched { get; set; }
                    public string? firstName { get; set; }
                    public string? lastName { get; set; }
                    public string? fatherName { get; set; }
                    public bool? alive { get; set; }
                }
            }
        }

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

        public class ShahkarInquiry
        {
            public class Request
            {
                public string? mobile { get; set; }
                public string? nationalCode { get; set; }
            }

            public class Response
            {
                public string? message { get; set; }
                public Data? data { get; set; }
                public int? result { get; set; }

                public class Data
                {
                    public bool? matched { get; set; }
                }
            }
        }
    }
}

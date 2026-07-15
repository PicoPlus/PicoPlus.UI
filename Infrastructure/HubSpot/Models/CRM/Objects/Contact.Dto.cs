namespace NovinCRM.Models.CRM.Objects;

public partial class Contact
{
#nullable disable
    public class Base
    {
        public class Response

        {
            public string id { get; set; }
            public Properties properties { get; set; }
            public DateTime createdAt { get; set; }
            public DateTime updatedAt { get; set; }
            public bool archived { get; set; }
            public class Properties
            {
                // -- Core identity -----------------------------------------
                public string firstname { get; set; }
                public string lastname { get; set; }
                public string email { get; set; }
                public string phone { get; set; }
                public string mobilephone { get; set; }
                public string fax { get; set; }
                public string salutation { get; set; }

                // -- Address -----------------------------------------------
                public string address { get; set; }
                public string city { get; set; }
                public string state { get; set; }
                public string zip { get; set; }
                public string country { get; set; }
                public string hs_country_region_code { get; set; }

                // -- Company / professional --------------------------------
                public string company { get; set; }
                public string jobtitle { get; set; }
                public string job_function { get; set; }
                public string numemployees { get; set; }
                public string annualrevenue { get; set; }
                public string industry { get; set; }
                public string website { get; set; }

                // -- Custom / app-specific ---------------------------------
                public string ncode { get; set; }
                public string dateofbirth { get; set; }
                public string bdate { get; set; }
                public string father_name { get; set; }
                public string fathername { get; set; }
                public string gender { get; set; }
                public string shahkar_status { get; set; }
                public string wallet { get; set; }
                public string contact_plan { get; set; }
                public string base_chat_id { get; set; }

                // -- Revenue / deals ---------------------------------------
                public string total_revenue { get; set; }
                public string num_associated_deals { get; set; }
                public string recent_deal_amount { get; set; }
                public string recent_deal_close_date { get; set; }

                // -- HubSpot system ----------------------------------------
                public string hs_object_id { get; set; }
                public string hubspot_owner_id { get; set; }
                public string hubspot_team_id { get; set; }
                public string hs_lead_status { get; set; }
                public string hs_lifecycle_stage { get; set; }
                public string lifecyclestage { get; set; }
                public string hs_language { get; set; }
                public string hs_timezone { get; set; }
                public string hs_whatsapp_phone_number { get; set; }
                public string hs_linkedin_url { get; set; }
                public string hs_createdate { get; set; }
                public string hs_lastmodifieddate { get; set; }
                public string createdate { get; set; }
                public string lastmodifieddate { get; set; }
                public string hubspot_owner_assigneddate { get; set; }

                // -- Notes / activity --------------------------------------
                public string notes_last_contacted { get; set; }
                public string notes_last_updated { get; set; }
                public string notes_next_activity_date { get; set; }
                public string num_contacted_notes { get; set; }
                public string num_notes { get; set; }

                // -- Geographic (IP-inferred) -------------------------------
                public string ip_city { get; set; }
                public string ip_country { get; set; }
                public string ip_state { get; set; }
                public string ip_zipcode { get; set; }

                /// <summary>Avatar / National Card Image URL</summary>
                public string last_products_bought_product_1_image_url { get; set; }
            }
        }
    }

    public class Search

    {
        public class Request
        {
            public string query { get; set; }
            public int limit { get; set; }
            public object[] sorts { get; set; }
            public string[] properties { get; set; }
            public FilterGroup[] filterGroups { get; set; }

            public class Filter
            {
                public string highValue { get; set; }
                public string propertyName { get; set; }
                public string value { get; set; }


                public string @operator { get; set; }
            }

            public class FilterGroup
            {
                public Filter[] filters { get; set; }
            }
        }
        public class Response

        {
            public int total { get; set; }

            public Paging paging { get; set; }

            public List<Result> results { get; set; }


            public class Paging
            {
                public Next next { get; set; }
            }

            public class Next
            {
                public string after { get; set; }
                public string link { get; set; }
            }

            public class Result
            {
                public string id { get; set; }
                public Properties properties { get; set; }
                public string createdAt { get; set; }
                public string updatedAt { get; set; }
                public bool archived { get; set; }
                public class Properties : Read.Response.Properties { }


            }


        }

    }

    public class Create
    {
        public class Request
        {
            public Properties? properties { get; set; }

            /// <summary>
            /// Only writable fields — never include read-only HubSpot system properties
            /// (hs_object_id, createdate, etc.) or the API returns 400.
            /// </summary>
            public class Properties
            {
                public string? firstname      { get; set; }
                public string? lastname       { get; set; }
                public string? email          { get; set; }
                public string? phone          { get; set; }
                public string? ncode          { get; set; }
                public string? bdate          { get; set; }
                public string? fathername     { get; set; }
                public string? gender         { get; set; }
                public string? shahkar_status { get; set; }
            }
        }

        public class Response : Base.Response { }
    }

    public class Read : Base

    {
        public class Response : Base.Response { public class Properties : Base.Response.Properties { } }
    }


}


















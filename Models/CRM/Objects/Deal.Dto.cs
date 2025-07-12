
using Newtonsoft.Json;

namespace PicoPlus.Models.CRM.Objects;

public partial class Deal
{

    public class Create
    {
        public class Request
        {
            public List<Association> associations { get; set; }
            public Properties properties { get; set; }
            public class Properties
            {
                public string amount { get; set; }
                public string dealname { get; set; }
                public string pipeline { get; set; }
                public long closedate { get; set; }
                public string dealstage { get; set; }
                public string hubspot_owner_id { get; set; }
                public string pos_tid { get; set; }
                public string hs_priority { get; set; }
                public string description { get; set; }

                public string payment_type { get; set; }

                public string payed_in_cash { get; set; }

                public string payed_in_c2c { get; set; }    

                public string payed_in_pos { get; set; }

            }

            public class Association
            {
                public To to { get; set; }
                public List<Type> types { get; set; }
            }

            public class To
            {
                public long id { get; set; }
            }

            public class Type
            {
                public string associationCategory { get; set; }
                public int associationTypeId { get; set; }
            }

        }

        public class Response
        {
            public string id { get; set; }
            public Properties properties { get; set; }
            public DateTime createdAt { get; set; }
            public DateTime updatedAt { get; set; }
            public bool archived { get; set; }

            public class Properties
            {
                public string amount { get; set; }
                public string amount_in_home_currency { get; set; }
                public DateTime closedate { get; set; }
                public DateTime createdate { get; set; }
                public string days_to_close { get; set; }
                public string dealname { get; set; }
                public string dealstage { get; set; }
                public object description { get; set; }
                public string hs_all_owner_ids { get; set; }
                public string hs_closed_amount { get; set; }
                public string hs_closed_amount_in_home_currency { get; set; }
                public string hs_closed_won_count { get; set; }
                public DateTime hs_closed_won_date { get; set; }
                public DateTime hs_createdate { get; set; }
                public string hs_days_to_close_raw { get; set; }
                public string hs_deal_stage_probability_shadow { get; set; }
                public string hs_forecast_amount { get; set; }
                public string hs_is_closed { get; set; }
                public string hs_is_closed_won { get; set; }
                public string hs_is_deal_split { get; set; }
                public string hs_is_open_count { get; set; }
                public DateTime hs_lastmodifieddate { get; set; }
                public string hs_object_id { get; set; }
                public string hs_object_source { get; set; }
                public string hs_object_source_id { get; set; }
                public string hs_object_source_label { get; set; }
                public string hs_projected_amount { get; set; }
                public string hs_projected_amount_in_home_currency { get; set; }
                public string hs_user_ids_of_all_owners { get; set; }
                public DateTime hubspot_owner_assigneddate { get; set; }
                public string hubspot_owner_id { get; set; }
                public string pipeline { get; set; }
                public object pos_tid { get; set; }
            }

        }
    }

    public class Get
    {
        public class Response
        {
            public class Associations
            {
                [JsonProperty("line items", PropertyName = "line items")]
                public LineItems lineitems { get; set; }
                public Contacts contacts { get; set; }
                public Notes notes { get; set; }
            }

            public class Contacts
            {
                public List<Result> results { get; set; }
            }

            public class LineItems
            {
                public List<Result1> results { get; set; }
            }
            public class Notes
            {
                public List<Result2> results { get; set; }
            }
            public class Properties
            {
                public string amount { get; set; }
                public DateTime closedate { get; set; }
                public DateTime createdate { get; set; }
                public string dealname { get; set; }
                public DateTime hs_lastmodifieddate { get; set; }
                public string hs_object_id { get; set; }
                public string pipeline { get; set; }
                public string dealstage { get; set; }
            }

            public class Result
            {
                public string id { get; set; }
                public string type { get; set; }
            }
            public class Result1
            {
                public string id { get; set; }
                public string type { get; set; }
            }

            public class Result2
            {
                public string id { get; set; }
                public string type { get; set; }
            }

            public string id { get; set; }
            public Properties properties { get; set; }
            public DateTime createdAt { get; set; }
            public DateTime updatedAt { get; set; }
            public bool archived { get; set; }
            public Associations associations { get; set; }



        }
    }

    public class GetBatch()
    {
        public class Request
        {
            public class Input
            {
                public string id { get; set; }
            }
            public List<string> propertiesWithHistory { get; set; }
            public List<Input> inputs { get; set; }
            public List<string> properties { get; set; }


        }

        public class Response
        {
            public class Properties
            {
                public string amount { get; set; }
                public string createdate { get; set; }
                public string hs_lastmodifieddate { get; set; }
                public string hs_object_id { get; set; }
                public string dealname { get; set; }
                public string dealstage { get; set; }
            }

            public class Result
            {
                public string id { get; set; }
                public Properties properties { get; set; }
                public Dictionary<string, string> propertiesWithHistory { get; set; }
                public DateTime createdAt { get; set; }
                public DateTime updatedAt { get; set; }
                public bool archived { get; set; }

            }

            public class Context
            {
                public List<string> ids { get; set; }
            }

            public class Error
            {
                public string status { get; set; }
                public string category { get; set; }
                public string message { get; set; }
                public Context context { get; set; }
            }
            public string status { get; set; }
            public List<Result> results { get; set; }
            public int numErrors { get; set; }
            public List<Error> errors { get; set; }
            public DateTime startedAt { get; set; }
            public DateTime completedAt { get; set; }

        }
    }


}


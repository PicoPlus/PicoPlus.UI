
using System.Text.Json.Serialization;

namespace PicoPlus.Models.CRM.Commerce
{
    public partial class LineItem
    {

        public class Create
        {
            public class Request
            {
                public List<Input>? inputs { get; set; }

                public class Input
                {
                    public List<Association>? associations { get; set; }
                    public Properties? properties { get; set; }
                }

                public class Properties
                {
                    public string? name { get; set; }
                    public decimal? price { get; set; }
                    public long? quantity { get; set; }
                    public string? hs_product_id { get; set; }
                    public string? recurringbillingfrequency { get; set; }
                    public string? hs_discount_percentage { get; set; }
                    public string? hs_recurring_billing_period { get; set; }
                    public string hs_sku { get;set; }

                    [JsonIgnore]
                    public decimal TotalPrice { get; set; }    
                    
                }

                public class Association
                {
                    public List<Type>? types { get; set; }
                    public To? to { get; set; }
                }

                public class To
                {
                    public string? id { get; set; }
                }

                public class Type
                {
                    public string? associationCategory { get; set; }
                    public int associationTypeId { get; set; }
                }

            }

            public class Response
            {

                public string? status { get; set; }

                public List<Result>? results { get; set; }

                public DateTime startedAt { get; set; }

                public DateTime completedAt { get; set; }


                public class Result
                {
                    public string? id { get; set; }
                    public Properties? properties { get; set; }
                    public DateTime createdAt { get; set; }
                    public DateTime updatedAt { get; set; }
                    public bool archived { get; set; }
                }

                public class Properties
                {
                    public string? amount { get; set; }
                    public DateTime createdate { get; set; }
                    public string? hs_acv { get; set; }
                    public string? hs_arr { get; set; }
                    public string? hs_discount_percentage { get; set; }
                    public DateTime hs_lastmodifieddate { get; set; }
                    public string? hs_margin { get; set; }
                    public string? hs_margin_acv { get; set; }
                    public string? hs_margin_arr { get; set; }
                    public string? hs_margin_mrr { get; set; }
                    public string? hs_margin_tcv { get; set; }
                    public string? hs_mrr { get; set; }
                    public string? hs_object_id { get; set; }
                    public string? hs_object_source { get; set; }
                    public string? hs_object_source_id { get; set; }
                    public string? hs_object_source_label { get; set; }
                    public string? hs_position_on_quote { get; set; }
                    public string? hs_post_tax_amount { get; set; }
                    public string? hs_pre_discount_amount { get; set; }
                    public string? hs_product_id { get; set; }
                    public string? hs_product_type { get; set; }
                    public string? hs_recurring_billing_number_of_payments { get; set; }
                    public string? hs_sku { get; set; }
                    public string? hs_tcv { get; set; }
                    public string? hs_total_discount { get; set; }
                    public string? name { get; set; }
                    public string? price { get; set; }
                    public string? quantity { get; set; }
                }

            }


        }

        public class Read
        {

            public class Response
            {

                public string? id { get; set; }
                public Properties? properties { get; set; }
                public DateTime createdAt { get; set; }
                public DateTime updatedAt { get; set; }
                public bool archived { get; set; }


                public class Properties
                {
                    public string? amount { get; set; }
                    public DateTime createdate { get; set; }
                    public string? hs_discount_percentage { get; set; }
                    public DateTime hs_lastmodifieddate { get; set; }
                    public string? hs_object_id { get; set; }
                    public string? hs_product_id { get; set; }
                    public string? name { get; set; }
                    public string? price { get; set; }
                    public string? quantity { get; set; }
                }


            }

        }

    }

}


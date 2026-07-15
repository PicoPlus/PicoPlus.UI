using System.Text.Json.Serialization;

namespace NovinCRM.Models.CRM.Commerce;

/// <summary>
/// DTOs for the HubSpot Invoices API (/crm/v3/objects/invoices).
/// </summary>
public partial class Invoice
{
    public class Create
    {
        public class Request
        {
            [JsonPropertyName("properties")]
            public Properties? properties { get; set; }

            [JsonPropertyName("associations")]
            public List<Association>? associations { get; set; }
        }

        public class Properties
        {
            [JsonPropertyName("hs_invoice_status")]
            public string? hs_invoice_status { get; set; }   // "DRAFT" | "OPEN"

            [JsonPropertyName("hs_title")]
            public string? hs_title { get; set; }

            [JsonPropertyName("hs_currency_code")]
            public string? hs_currency_code { get; set; }    // "IRR"

            [JsonPropertyName("hs_due_date")]
            public string? hs_due_date { get; set; }         // yyyy-MM-dd

            [JsonPropertyName("hs_invoice_source")]
            public string? hs_invoice_source { get; set; }   // "API"
        }

        public class Association
        {
            [JsonPropertyName("to")]
            public To? to { get; set; }

            [JsonPropertyName("types")]
            public List<AssocType>? types { get; set; }
        }

        public class To
        {
            [JsonPropertyName("id")]
            public string? id { get; set; }
        }

        public class AssocType
        {
            [JsonPropertyName("associationCategory")]
            public string? associationCategory { get; set; }

            [JsonPropertyName("associationTypeId")]
            public int associationTypeId { get; set; }
        }

        public class Response
        {
            [JsonPropertyName("id")]
            public string? id { get; set; }

            [JsonPropertyName("properties")]
            public Properties? properties { get; set; }

            [JsonPropertyName("createdAt")]
            public DateTime createdAt { get; set; }
        }
    }

    /// <summary>Batch association request body for /crm/v4/associations/.../batch/create</summary>
    public class BatchAssociateRequest
    {
        [JsonPropertyName("inputs")]
        public List<BatchAssociateInput>? inputs { get; set; }
    }

    public class BatchAssociateInput
    {
        [JsonPropertyName("from")]
        public BatchAssociateId? from { get; set; }

        [JsonPropertyName("to")]
        public BatchAssociateId? to { get; set; }

        [JsonPropertyName("types")]
        public List<Create.AssocType>? types { get; set; }
    }

    public class BatchAssociateId
    {
        [JsonPropertyName("id")]
        public string? id { get; set; }
    }
}

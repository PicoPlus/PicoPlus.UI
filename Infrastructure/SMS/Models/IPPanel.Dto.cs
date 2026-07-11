#nullable enable

using Newtonsoft.Json;

namespace PicoPlus.SMS.Models.IPPanel;

// ── Send pattern (OTP / notifications) ───────────────────────────────────────
// IPPanel Edge API — POST /api/send  (sending_type = "pattern")
// Docs: https://github.com/ippanelcom/Edge-Document

public class SendPatternRequest
{
    /// <summary>Must be "pattern" for pattern sends.</summary>
    [JsonProperty("sending_type")]
    public string SendingType { get; set; } = "pattern";

    /// <summary>Sender line number in E.164 format, e.g. +983000505.</summary>
    [JsonProperty("from_number")]
    public string? FromNumber { get; set; }

    /// <summary>Pattern code assigned in the IPPanel panel.</summary>
    [JsonProperty("code")]
    public string? Code { get; set; }

    /// <summary>Exactly one recipient in E.164 format, e.g. +989120000000.</summary>
    [JsonProperty("recipients")]
    public List<string>? Recipients { get; set; }

    /// <summary>Key–value pairs whose keys match the named placeholders in the pattern.</summary>
    [JsonProperty("params")]
    public Dictionary<string, string>? Params { get; set; }

    /// <summary>Optional phonebook entry — omit when not needed.</summary>
    [JsonProperty("phonebook")]
    public PhonebookEntry? Phonebook { get; set; }
}

public class PhonebookEntry
{
    [JsonProperty("id")]
    public int Id { get; set; }

    [JsonProperty("name")]
    public string? Name { get; set; }

    [JsonProperty("pre")]
    public string? Pre { get; set; }

    [JsonProperty("email")]
    public string? Email { get; set; }

    [JsonProperty("options")]
    public Dictionary<string, string>? Options { get; set; }
}

public class SendPatternResponse
{
    [JsonProperty("data")]
    public SendPatternData? Data { get; set; }

    [JsonProperty("meta")]
    public ResponseMeta? Meta { get; set; }
}

public class SendPatternData
{
    [JsonProperty("message_outbox_ids")]
    public List<long>? MessageOutboxIds { get; set; }
}

public class ResponseMeta
{
    [JsonProperty("status")]
    public bool Status { get; set; }

    [JsonProperty("message")]
    public string? Message { get; set; }

    [JsonProperty("message_code")]
    public string? MessageCode { get; set; }
}

// ── Check token ───────────────────────────────────────────────────────────────

public class CheckTokenResponse
{
    [JsonProperty("data")]
    public CheckTokenData? Data { get; set; }

    [JsonProperty("meta")]
    public ResponseMeta? Meta { get; set; }
}

public class CheckTokenData
{
    [JsonProperty("user_name")]
    public string? UserName { get; set; }

    [JsonProperty("user_id")]
    public int UserId { get; set; }

    [JsonProperty("name")]
    public string? Name { get; set; }

    [JsonProperty("send_block")]
    public bool SendBlock { get; set; }

    [JsonProperty("document_block")]
    public bool DocumentBlock { get; set; }
}

// ── Plain text send ───────────────────────────────────────────────────────────

public class SendRequest
{
    public string? receptor { get; set; }
    public string? sender   { get; set; }
    public string? message  { get; set; }
}

public class SendResponse
{
    public int     status  { get; set; }
    public string? message { get; set; }
    public SendData? data  { get; set; }
}

public class SendData
{
    public string? messageId { get; set; }
}

// ── Credit ────────────────────────────────────────────────────────────────────

public class CreditResponse
{
    public int    status  { get; set; }
    public string? message { get; set; }
    public CreditData? data { get; set; }
}

public class CreditData
{
    public decimal credit   { get; set; }
    public string? currency { get; set; }
}

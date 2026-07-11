#nullable enable

namespace PicoPlus.Models.Services.SMS;

/// <summary>
/// SMS.ir API Data Transfer Objects
/// Documentation: https://docs.sms.ir/
/// Base URL: https://api.sms.ir/
/// Authentication: x-api-key header
/// </summary>
public class SmsIr
{
    #region Common Models

    /// <summary>
    /// Base response model for SMS.ir API
    /// </summary>
    public class BaseResponse
    {
        /// <summary>HTTP status code</summary>
        public int status { get; set; }

        /// <summary>Response message</summary>
        public string? message { get; set; }
    }

    /// <summary>
    /// Base response with data
    /// </summary>
    public class BaseResponse<T> : BaseResponse
    {
        /// <summary>Response data</summary>
        public T? data { get; set; }
    }

    #endregion

    #region Send SMS (????? ?????)

    /// <summary>
    /// Send single SMS message
    /// Endpoint: POST /v1/send
    /// </summary>
    public class SendSms
    {
        public class Request
        {
            /// <summary>????? ?? ????? ?????</summary>
            public string? lineNumber { get; set; }

            /// <summary>????? ??????</summary>
            public string? mobile { get; set; }

            /// <summary>??? ????</summary>
            public string? messageText { get; set; }

            /// <summary>???? ????? (??????? - Unix timestamp)</summary>
            public long? sendDateTime { get; set; }
        }

        public class Response : BaseResponse<ResponseData>
        {
        }

        public class ResponseData
        {
            /// <summary>????? ????</summary>
            public long? messageId { get; set; }

            /// <summary>????? ?????</summary>
            public decimal? cost { get; set; }
        }
    }

    #endregion

    #region Send Bulk SMS (????? ?????)

    /// <summary>
    /// Send bulk SMS messages
    /// Endpoint: POST /v1/send/bulk
    /// </summary>
    public class SendBulk
    {
        public class Request
        {
            /// <summary>????? ?? ????? ?????</summary>
            public string? lineNumber { get; set; }

            /// <summary>??? ????</summary>
            public string? messageText { get; set; }

            /// <summary>???? ????? ????????</summary>
            public List<string>? mobiles { get; set; }

            /// <summary>???? ????? (??????? - Unix timestamp)</summary>
            public long? sendDateTime { get; set; }
        }

        public class Response : BaseResponse<ResponseData>
        {
        }

        public class ResponseData
        {
            /// <summary>???? ????? ???????</summary>
            public List<long>? messageIds { get; set; }

            /// <summary>????? ?? ?????</summary>
            public decimal? cost { get; set; }

            /// <summary>????? ???????</summary>
            public int? count { get; set; }
        }
    }

    #endregion

    #region Send Like To Like (????? ?? ?? ??)

    /// <summary>
    /// Send individual messages to different recipients
    /// Endpoint: POST /v1/send/likeToLike
    /// </summary>
    public class SendLikeToLike
    {
        public class Request
        {
            /// <summary>????? ?? ????? ?????</summary>
            public string? lineNumber { get; set; }

            /// <summary>???? ???????</summary>
            public List<MessageItem>? messages { get; set; }

            /// <summary>???? ????? (??????? - Unix timestamp)</summary>
            public long? sendDateTime { get; set; }
        }

        public class MessageItem
        {
            /// <summary>????? ??????</summary>
            public string? mobile { get; set; }

            /// <summary>??? ????</summary>
            public string? messageText { get; set; }
        }

        public class Response : BaseResponse<ResponseData>
        {
        }

        public class ResponseData
        {
            /// <summary>???? ????? ???????</summary>
            public List<long>? messageIds { get; set; }

            /// <summary>????? ?? ?????</summary>
            public decimal? cost { get; set; }

            /// <summary>????? ???????</summary>
            public int? count { get; set; }
        }
    }

    #endregion

    #region Send Verify/Pattern (????? ?? ????? - ????)

    /// <summary>
    /// Send pattern-based SMS (OTP, notifications, etc.)
    /// Endpoint: POST /v1/send/verify
    /// </summary>
    public class SendVerify
    {
        public class Request
        {
            /// <summary>????? ?????? ??????</summary>
            public string? mobile { get; set; }

            /// <summary>?? ????</summary>
            public int? templateId { get; set; }

            /// <summary>?????????? ????</summary>
            public List<ParameterItem>? parameters { get; set; }
        }

        public class ParameterItem
        {
            /// <summary>??? ???????</summary>
            public string? name { get; set; }

            /// <summary>????? ???????</summary>
            public string? value { get; set; }
        }

        public class Response : BaseResponse<ResponseData>
        {
        }

        public class ResponseData
        {
            /// <summary>????? ????</summary>
            public long? messageId { get; set; }

            /// <summary>????? ?????</summary>
            public decimal? cost { get; set; }
        }
    }

    #endregion

    #region Send Verify Bulk (????? ????? ????)

    /// <summary>
    /// Send pattern-based SMS to multiple recipients
    /// Endpoint: POST /v1/send/verify/bulk
    /// </summary>
    public class SendVerifyBulk
    {
        public class Request
        {
            /// <summary>?? ????</summary>
            public int? templateId { get; set; }

            /// <summary>???? ???????</summary>
            public List<MessageItem>? messages { get; set; }
        }

        public class MessageItem
        {
            /// <summary>????? ?????? ??????</summary>
            public string? mobile { get; set; }

            /// <summary>?????????? ????</summary>
            public List<SendVerify.ParameterItem>? parameters { get; set; }
        }

        public class Response : BaseResponse<ResponseData>
        {
        }

        public class ResponseData
        {
            /// <summary>???? ????? ???????</summary>
            public List<long>? messageIds { get; set; }

            /// <summary>????? ?? ?????</summary>
            public decimal? cost { get; set; }

            /// <summary>????? ???????</summary>
            public int? count { get; set; }
        }
    }

    #endregion

    #region Credit (?????? ????)

    /// <summary>
    /// Get account credit balance
    /// Endpoint: GET /v1/credit
    /// </summary>
    public class Credit
    {
        public class Response : BaseResponse<ResponseData>
        {
        }

        public class ResponseData
        {
            /// <summary>?????? ???? (????)</summary>
            public decimal? credit { get; set; }
        }
    }

    #endregion

    #region Lines (????)

    /// <summary>
    /// Get list of SMS lines
    /// Endpoint: GET /v1/line/list
    /// </summary>
    public class Lines
    {
        public class Response : BaseResponse<ResponseData>
        {
        }

        public class ResponseData
        {
            /// <summary>???? ????</summary>
            public List<LineItem>? lines { get; set; }
        }

        public class LineItem
        {
            /// <summary>????? ??</summary>
            public string? lineNumber { get; set; }

            /// <summary>??? ??</summary>
            public string? type { get; set; }

            /// <summary>????? ???? ????</summary>
            public bool? isActive { get; set; }

            /// <summary>???????</summary>
            public string? description { get; set; }
        }
    }

    #endregion

    #region Reports (???????)

    /// <summary>
    /// Get report for a specific message
    /// Endpoint: GET /v1/report/{messageId}
    /// </summary>
    public class Report
    {
        public class Response : BaseResponse<ResponseData>
        {
        }

        public class ResponseData
        {
            /// <summary>????? ????</summary>
            public long? messageId { get; set; }

            /// <summary>????? ??????</summary>
            public string? mobile { get; set; }

            /// <summary>????? ?? ????? ?????</summary>
            public string? lineNumber { get; set; }

            /// <summary>??? ????</summary>
            public string? messageText { get; set; }

            /// <summary>????? ?????</summary>
            public int? status { get; set; }

            /// <summary>??? ?????</summary>
            public string? statusText { get; set; }

            /// <summary>???? ????? (Unix timestamp)</summary>
            public long? sendDateTime { get; set; }

            /// <summary>???? ?????? (Unix timestamp)</summary>
            public long? deliveryDateTime { get; set; }

            /// <summary>?????</summary>
            public decimal? cost { get; set; }
        }
    }

    #endregion

    #region Bulk Reports (????? ?????)

    /// <summary>
    /// Get reports for multiple messages
    /// Endpoint: POST /v1/report/live
    /// </summary>
    public class BulkReport
    {
        public class Request
        {
            /// <summary>???? ????? ???????</summary>
            public List<long>? messageIds { get; set; }
        }

        public class Response : BaseResponse<ResponseData>
        {
        }

        public class ResponseData
        {
            /// <summary>???? ????????</summary>
            public List<Report.ResponseData>? reports { get; set; }
        }
    }

    #endregion

    #region Archived Reports (????? ???????)

    /// <summary>
    /// Get archived message reports by date range
    /// Endpoint: POST /v1/report/archived
    /// </summary>
    public class ArchivedReport
    {
        public class Request
        {
            /// <summary>????? ???? (Unix timestamp)</summary>
            public long? fromDate { get; set; }

            /// <summary>????? ????? (Unix timestamp)</summary>
            public long? toDate { get; set; }

            /// <summary>????? ????</summary>
            public int? page { get; set; }

            /// <summary>????? ?? ?? ????</summary>
            public int? pageSize { get; set; }
        }

        public class Response : BaseResponse<ResponseData>
        {
        }

        public class ResponseData
        {
            /// <summary>???? ????????</summary>
            public List<Report.ResponseData>? reports { get; set; }

            /// <summary>????? ??</summary>
            public int? totalCount { get; set; }

            /// <summary>????? ????</summary>
            public int? page { get; set; }

            /// <summary>????? ?? ?? ????</summary>
            public int? pageSize { get; set; }
        }
    }

    #endregion

    #region Latest Archived (????? ???????)

    /// <summary>
    /// Get latest archived messages
    /// Endpoint: GET /v1/report/archived/latest
    /// </summary>
    public class LatestArchived
    {
        public class Response : BaseResponse<ResponseData>
        {
        }

        public class ResponseData
        {
            /// <summary>???? ????????</summary>
            public List<Report.ResponseData>? reports { get; set; }
        }
    }

    #endregion

    #region Received Messages (???????? ???????)

    /// <summary>
    /// Get received messages
    /// Endpoint: POST /v1/receive/live
    /// </summary>
    public class ReceivedMessages
    {
        public class Request
        {
            /// <summary>????? ???????</summary>
            public int? count { get; set; }
        }

        public class Response : BaseResponse<ResponseData>
        {
        }

        public class ResponseData
        {
            /// <summary>???? ???????? ???????</summary>
            public List<ReceivedMessageItem>? messages { get; set; }
        }

        public class ReceivedMessageItem
        {
            /// <summary>????? ????</summary>
            public long? messageId { get; set; }

            /// <summary>????? ???????</summary>
            public string? senderNumber { get; set; }

            /// <summary>????? ?? ??????</summary>
            public string? receiverNumber { get; set; }

            /// <summary>??? ????</summary>
            public string? messageText { get; set; }

            /// <summary>???? ?????? (Unix timestamp)</summary>
            public long? receiveDateTime { get; set; }
        }
    }

    #endregion

    #region Archived Received Messages (???????? ??????? ???????)

    /// <summary>
    /// Get archived received messages by date range
    /// Endpoint: POST /v1/receive/archived
    /// </summary>
    public class ArchivedReceived
    {
        public class Request
        {
            /// <summary>????? ???? (Unix timestamp)</summary>
            public long? fromDate { get; set; }

            /// <summary>????? ????? (Unix timestamp)</summary>
            public long? toDate { get; set; }

            /// <summary>????? ????</summary>
            public int? page { get; set; }

            /// <summary>????? ?? ?? ????</summary>
            public int? pageSize { get; set; }
        }

        public class Response : BaseResponse<ResponseData>
        {
        }

        public class ResponseData
        {
            /// <summary>???? ???????? ???????</summary>
            public List<ReceivedMessages.ReceivedMessageItem>? messages { get; set; }

            /// <summary>????? ??</summary>
            public int? totalCount { get; set; }

            /// <summary>????? ????</summary>
            public int? page { get; set; }

            /// <summary>????? ?? ?? ????</summary>
            public int? pageSize { get; set; }
        }
    }

    #endregion

    #region Latest Received (????? ???????)

    /// <summary>
    /// Get latest received messages
    /// Endpoint: GET /v1/receive/latest
    /// </summary>
    public class LatestReceived
    {
        public class Response : BaseResponse<ResponseData>
        {
        }

        public class ResponseData
        {
            /// <summary>???? ???????? ???????</summary>
            public List<ReceivedMessages.ReceivedMessageItem>? messages { get; set; }
        }
    }

    #endregion

    #region Delete Scheduled (??? ???? ????????? ???)

    /// <summary>
    /// Delete scheduled message
    /// Endpoint: DELETE /v1/send/scheduled/{messageId}
    /// </summary>
    public class DeleteScheduled
    {
        public class Response : BaseResponse
        {
        }
    }

    #endregion

    #region Templates (??????)

    /// <summary>
    /// Get list of templates/patterns
    /// Endpoint: GET /v1/template/list
    /// </summary>
    public class Templates
    {
        public class Response : BaseResponse<ResponseData>
        {
        }

        public class ResponseData
        {
            /// <summary>???? ??????</summary>
            public List<TemplateItem>? templates { get; set; }
        }

        public class TemplateItem
        {
            /// <summary>????? ????</summary>
            public int? templateId { get; set; }

            /// <summary>????? ????</summary>
            public string? title { get; set; }

            /// <summary>??? ????</summary>
            public string? text { get; set; }

            /// <summary>????? ????</summary>
            public string? status { get; set; }

            /// <summary>???? ?????????</summary>
            public List<string>? parameters { get; set; }
        }
    }

    #endregion

    #region Message Status Codes

    /// <summary>
    /// SMS.ir message status codes
    /// </summary>
    public static class MessageStatus
    {
        public const int Pending = 0;           // ?? ?? ?????
        public const int Delivered = 1;         // ????? ???
        public const int Failed = 2;            // ????? ????
        public const int Sending = 3;           // ?? ??? ?????
        public const int DeliveredToOperator = 4; // ????? ?? ???????
        public const int NotDelivered = 5;      // ??? ?????
        public const int Cancelled = 6;         // ??? ???
        public const int Blocked = 8;           // ???? ???
        public const int InvalidNumber = 10;    // ????? ???????
        public const int FilteredContent = 11;  // ?????? ????? ???
        public const int SpamReported = 13;     // ????? ????
        public const int Expired = 14;          // ????? ???
    }

    #endregion

    #region Helper Extensions

    /// <summary>
    /// Helper methods for SMS.ir status
    /// </summary>
    public static class StatusHelpers
    {
        public static string GetStatusText(int status)
        {
            return status switch
            {
                0 => "?? ?? ?????",
                1 => "????? ??? ?? ??????",
                2 => "????? ????",
                3 => "?? ??? ?????",
                4 => "????? ???? ??? ?? ???????",
                5 => "??? ????? ?? ??????",
                6 => "??? ???",
                8 => "???? ??? ???? ???????",
                10 => "????? ???????",
                11 => "?????? ???? ????? ???",
                13 => "????? ????",
                14 => "????? ???",
                _ => "????? ??????"
            };
        }

        public static bool IsSuccessful(int status)
        {
            return status == MessageStatus.Delivered;
        }

        public static bool IsFailed(int status)
        {
            return status is MessageStatus.Failed
                or MessageStatus.NotDelivered
                or MessageStatus.Cancelled
                or MessageStatus.Blocked
                or MessageStatus.InvalidNumber
                or MessageStatus.FilteredContent
                or MessageStatus.Expired;
        }

        public static bool IsPending(int status)
        {
            return status is MessageStatus.Pending
                or MessageStatus.Sending
                or MessageStatus.DeliveredToOperator;
        }
    }

    #endregion
}

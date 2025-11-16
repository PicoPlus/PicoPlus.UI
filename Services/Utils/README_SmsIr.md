# SMS.ir Integration Guide

## Overview

SMS.ir is a comprehensive Iranian SMS service provider integrated into the PicoPlus application. This service provides full support for sending SMS messages, pattern-based notifications, OTP codes, reports, and account management.

## Configuration

### appsettings.json

Add the following configuration to your `appsettings.json`:

```json
{
  "SmsIr": {
    "ApiKey": "your-api-key-here",
    "Templates": {
      "OTP": "123456",
      "Welcome": "789012",
      "DealClosed": "345678"
    }
  }
}
```

### Environment Variables (Recommended for Production)

For production environments, use environment variables:

```bash
SMSIR_API_KEY=your-api-key-here
SMSIR_OTP_TEMPLATE_ID=123456
SMSIR_WELCOME_TEMPLATE_ID=789012
SMSIR_DEAL_CLOSED_TEMPLATE_ID=345678
```

## Dependency Injection Setup

### Program.cs

Add SMS.ir service to your dependency injection container:

```csharp
// Add HttpClient for SMS.ir
builder.Services.AddHttpClient<PicoPlus.Services.SMS.SmsIr>();

// Or configure with custom settings
builder.Services.AddHttpClient<PicoPlus.Services.SMS.SmsIr>(client =>
{
    client.Timeout = TimeSpan.FromSeconds(30);
    client.DefaultRequestHeaders.Add("User-Agent", "PicoPlus-App");
});
```

## Usage Examples

### 1. Send OTP Code

```csharp
@inject PicoPlus.Services.SMS.SmsIr _smsIr

private async Task SendOtpAsync()
{
    try
    {
        var response = await _smsIr.SendOtpAsync(
            mobile: "09123456789",
            otpCode: "123456"
        );
        
        if (response.status == 200)
        {
            Console.WriteLine($"OTP sent successfully. Message ID: {response.data?.messageId}");
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Error sending OTP: {ex.Message}");
    }
}
```

### 2. Send Welcome Message

```csharp
private async Task SendWelcomeAsync()
{
    try
    {
        var response = await _smsIr.SendWelcomeAsync(
            mobile: "09123456789",
            firstName: "???",
            lastName: "?????"
        );
        
        if (response.status == 200)
        {
            Console.WriteLine("Welcome message sent successfully");
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Error: {ex.Message}");
    }
}
```

### 3. Send Custom Pattern Message

```csharp
private async Task SendCustomNotificationAsync()
{
    var parameters = new Dictionary<string, string>
    {
        { "Name", "??? ?????" },
        { "Amount", "1,000,000" },
        { "Date", "1403/01/15" }
    };
    
    var response = await _smsIr.SendNotificationAsync(
        mobile: "09123456789",
        templateId: 123456,
        parameters: parameters
    );
}
```

### 4. Send Single SMS

```csharp
private async Task SendSingleSmsAsync()
{
    var request = new Models.Services.SMS.SmsIr.SendSms.Request
    {
        lineNumber = "30007732999900",
        mobile = "09123456789",
        messageText = "???? ????"
    };
    
    var response = await _smsIr.SendSmsAsync(request);
}
```

### 5. Send Bulk SMS

```csharp
private async Task SendBulkSmsAsync()
{
    var request = new Models.Services.SMS.SmsIr.SendBulk.Request
    {
        lineNumber = "30007732999900",
        messageText = "???? ????? ????",
        mobiles = new List<string>
        {
            "09123456789",
            "09121111111",
            "09122222222"
        }
    };
    
    var response = await _smsIr.SendBulkAsync(request);
    
    if (response.status == 200)
    {
        Console.WriteLine($"Sent {response.data?.count} messages");
        Console.WriteLine($"Total cost: {response.data?.cost} Rials");
    }
}
```

### 6. Check Message Status

```csharp
private async Task CheckMessageStatusAsync(long messageId)
{
    var report = await _smsIr.GetReportAsync(messageId);
    
    if (report.status == 200 && report.data != null)
    {
        var statusText = Models.Services.SMS.SmsIr.StatusHelpers.GetStatusText(
            report.data.status ?? 0
        );
        
        Console.WriteLine($"Status: {statusText}");
        Console.WriteLine($"Mobile: {report.data.mobile}");
        Console.WriteLine($"Cost: {report.data.cost}");
    }
}
```

### 7. Get Account Credit

```csharp
private async Task CheckCreditAsync()
{
    var response = await _smsIr.GetCreditAsync();
    
    if (response.status == 200)
    {
        Console.WriteLine($"Credit: {response.data?.credit:N0} Rials");
    }
}
```

### 8. Get SMS Lines

```csharp
private async Task GetSmsLinesAsync()
{
    var response = await _smsIr.GetLinesAsync();
    
    if (response.status == 200 && response.data?.lines != null)
    {
        foreach (var line in response.data.lines)
        {
            Console.WriteLine($"Line: {line.lineNumber}");
            Console.WriteLine($"Type: {line.type}");
            Console.WriteLine($"Active: {line.isActive}");
            Console.WriteLine("---");
        }
    }
}
```

### 9. Get Templates

```csharp
private async Task GetTemplatesAsync()
{
    var response = await _smsIr.GetTemplatesAsync();
    
    if (response.status == 200 && response.data?.templates != null)
    {
        foreach (var template in response.data.templates)
        {
            Console.WriteLine($"ID: {template.templateId}");
            Console.WriteLine($"Title: {template.title}");
            Console.WriteLine($"Status: {template.status}");
            Console.WriteLine($"Parameters: {string.Join(", ", template.parameters ?? new List<string>())}");
            Console.WriteLine("---");
        }
    }
}
```

### 10. Get Received Messages

```csharp
private async Task GetReceivedMessagesAsync()
{
    var request = new Models.Services.SMS.SmsIr.ReceivedMessages.Request
    {
        count = 100
    };
    
    var response = await _smsIr.GetReceivedMessagesAsync(request);
    
    if (response.status == 200 && response.data?.messages != null)
    {
        foreach (var msg in response.data.messages)
        {
            Console.WriteLine($"From: {msg.senderNumber}");
            Console.WriteLine($"Text: {msg.messageText}");
            Console.WriteLine($"Time: {DateTimeOffset.FromUnixTimeSeconds(msg.receiveDateTime ?? 0)}");
            Console.WriteLine("---");
        }
    }
}
```

## Message Status Codes

| Code | Description (Persian) | Description (English) |
|------|----------------------|----------------------|
| 0 | ?? ?? ????? | Pending |
| 1 | ????? ??? ?? ?????? | Delivered |
| 2 | ????? ???? | Failed |
| 3 | ?? ??? ????? | Sending |
| 4 | ????? ???? ??? ?? ??????? | Delivered to Operator |
| 5 | ??? ????? ?? ?????? | Not Delivered |
| 6 | ??? ??? | Cancelled |
| 8 | ???? ??? ???? ??????? | Blocked by Operator |
| 10 | ????? ??????? | Invalid Number |
| 11 | ?????? ???? ????? ??? | Filtered Content |
| 13 | ????? ???? | Spam Reported |
| 14 | ????? ??? | Expired |

### Status Helper Methods

```csharp
// Get status text
var statusText = Models.Services.SMS.SmsIr.StatusHelpers.GetStatusText(1);
// Output: "????? ??? ?? ??????"

// Check if successful
bool isSuccess = Models.Services.SMS.SmsIr.StatusHelpers.IsSuccessful(1); // true

// Check if failed
bool isFailed = Models.Services.SMS.SmsIr.StatusHelpers.IsFailed(2); // true

// Check if pending
bool isPending = Models.Services.SMS.SmsIr.StatusHelpers.IsPending(0); // true
```

## MVVM Integration Example

### ViewModel

```csharp
public class NotificationViewModel : ViewModelBase
{
    private readonly PicoPlus.Services.SMS.SmsIr _smsIr;
    
    public NotificationViewModel(PicoPlus.Services.SMS.SmsIr smsIr)
    {
        _smsIr = smsIr;
        SendOtpCommand = new AsyncRelayCommand(SendOtpAsync);
    }
    
    public IAsyncRelayCommand SendOtpCommand { get; }
    
    private string _mobile = string.Empty;
    public string Mobile
    {
        get => _mobile;
        set => SetProperty(ref _mobile, value);
    }
    
    private string _otpCode = string.Empty;
    public string OtpCode
    {
        get => _otpCode;
        set => SetProperty(ref _otpCode, value);
    }
    
    private async Task SendOtpAsync()
    {
        try
        {
            IsLoading = true;
            ErrorMessage = string.Empty;
            
            var response = await _smsIr.SendOtpAsync(Mobile, OtpCode);
            
            if (response.status == 200)
            {
                SuccessMessage = "?? ????? ?? ?????? ????? ??";
            }
            else
            {
                ErrorMessage = response.message ?? "??? ?? ????? ????";
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = $"???: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }
}
```

### View (Razor Component)

```razor
@page "/notifications"
@inject NotificationViewModel ViewModel

<div class="container">
    <h3>????? ?? ?????</h3>
    
    <EditForm Model="@ViewModel" OnValidSubmit="@HandleSendOtp">
        <div class="mb-3">
            <label>????? ??????</label>
            <InputText @bind-value="ViewModel.Mobile" class="form-control" />
        </div>
        
        <div class="mb-3">
            <label>?? ?????</label>
            <InputText @bind-value="ViewModel.OtpCode" class="form-control" />
        </div>
        
        <button type="submit" class="btn btn-primary" disabled="@ViewModel.IsLoading">
            @if (ViewModel.IsLoading)
            {
                <span class="spinner-border spinner-border-sm"></span>
            }
            ?????
        </button>
    </EditForm>
    
    @if (!string.IsNullOrEmpty(ViewModel.ErrorMessage))
    {
        <div class="alert alert-danger mt-3">@ViewModel.ErrorMessage</div>
    }
    
    @if (!string.IsNullOrEmpty(ViewModel.SuccessMessage))
    {
        <div class="alert alert-success mt-3">@ViewModel.SuccessMessage</div>
    }
</div>

@code {
    private async Task HandleSendOtp()
    {
        await ViewModel.SendOtpCommand.ExecuteAsync(null);
    }
}
```

## Error Handling

```csharp
try
{
    var response = await _smsIr.SendOtpAsync(mobile, code);
    
    if (response.status == 200)
    {
        // Success
    }
    else
    {
        // Handle error
        _logger.LogWarning("SMS.ir error: {Message}", response.message);
    }
}
catch (HttpRequestException ex)
{
    // Network error
    _logger.LogError(ex, "Network error calling SMS.ir");
}
catch (InvalidOperationException ex)
{
    // Configuration error
    _logger.LogError(ex, "SMS.ir configuration error");
}
catch (Exception ex)
{
    // Other errors
    _logger.LogError(ex, "Unexpected error in SMS.ir");
}
```

## Best Practices

1. **API Key Security**: Always use environment variables in production
2. **Error Handling**: Implement comprehensive error handling
3. **Logging**: Log all SMS operations for audit trails
4. **Rate Limiting**: Implement rate limiting to prevent abuse
5. **Template Management**: Store template IDs in configuration
6. **Testing**: Use test mode during development
7. **Cost Monitoring**: Regularly check credit balance
8. **Retry Logic**: Implement retry logic for failed messages

## API Documentation

For complete API documentation, visit: https://docs.sms.ir/

## Support

SMS.ir Support:
- Website: https://sms.ir
- Support: https://sms.ir/support
- Documentation: https://docs.sms.ir/

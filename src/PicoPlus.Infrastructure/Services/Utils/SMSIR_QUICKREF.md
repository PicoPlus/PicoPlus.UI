# SMS.ir Quick Reference

## ?? Quick Setup

### 1. Add to Program.cs
```csharp
// Register SMS services
builder.Services.AddHttpClient<PicoPlus.Services.SMS.SmsIr>()
    .ConfigurePrimaryHttpMessageHandler(() => new PicoPlus.Infrastructure.Http.ShecanDnsHttpClientHandler());

builder.Services.AddScoped<PicoPlus.Services.SMS.SMS.Send>();
builder.Services.AddScoped<PicoPlus.Services.SMS.SmsIrService>();
builder.Services.AddScoped<PicoPlus.Services.SMS.FarazSmsService>();
builder.Services.AddScoped<PicoPlus.Services.SMS.SmsServiceFactory>();
builder.Services.AddScoped<PicoPlus.Services.SMS.ISmsService, PicoPlus.Services.SMS.SmsService>();
```

### 2. Add to appsettings.json
```json
{
  "SMS": { "Provider": "SmsIr" },
  "SmsIr": {
    "ApiKey": "YOUR_API_KEY",
    "Templates": {
      "OTP": "764597",
      "Welcome": "789012"
    }
  }
}
```

### 3. Environment Variables (Production)
```bash
SMS_PROVIDER=SmsIr
SMSIR_API_KEY=your-key-here
SMSIR_OTP_TEMPLATE_ID=764597
```

## ?? Common Use Cases

### Send OTP
```csharp
@inject ISmsService _smsService

// Template ID: 764597
// Parameter name: OTP
await _smsService.SendOtpAsync("09123456789", "123456");
```

### Send Welcome
```csharp
await _smsService.SendWelcomeAsync("09123456789", "???", "?????", "CID123");
```

### Send Deal Notification
```csharp
await _smsService.SendDealClosedAsync("09123456789", "???", "?????", "DEAL456");
```

### Check Credit (SMS.ir only)
```csharp
@inject SmsIr _smsIr

var response = await _smsIr.GetCreditAsync();
Console.WriteLine($"Credit: {response.data?.credit:N0} Rials");
```

### Send Bulk SMS (SMS.ir only)
```csharp
var request = new Models.Services.SMS.SmsIr.SendBulk.Request
{
    lineNumber = "30007732999900",
    messageText = "Your message",
    mobiles = new List<string> { "09121111111", "09122222222" }
};

var response = await _smsIr.SendBulkAsync(request);
```

### Track Message Status (SMS.ir only)
```csharp
var report = await _smsIr.GetReportAsync(messageId);
var statusText = Models.Services.SMS.SmsIr.StatusHelpers.GetStatusText(report.data?.status ?? 0);
var isDelivered = Models.Services.SMS.SmsIr.StatusHelpers.IsSuccessful(report.data?.status ?? 0);
```

## ??? MVVM Pattern

### ViewModel
```csharp
public class MyViewModel : ViewModelBase
{
    private readonly ISmsService _smsService;
    
    public MyViewModel(ISmsService smsService)
    {
        _smsService = smsService;
        SendCommand = new AsyncRelayCommand(SendSmsAsync);
    }
    
    public IAsyncRelayCommand SendCommand { get; }
    
    private async Task SendSmsAsync()
    {
        try
        {
            IsLoading = true;
            await _smsService.SendOtpAsync(Phone, OtpCode);
            SuccessMessage = "???? ????? ??";
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
        }
        finally
        {
            IsLoading = false;
        }
    }
}
```

### View
```razor
@page "/send-sms"
@inject MyViewModel ViewModel

<button @onclick="@(() => ViewModel.SendCommand.ExecuteAsync(null))" 
        disabled="@ViewModel.IsLoading">
    @if (ViewModel.IsLoading)
    {
        <span class="spinner-border spinner-border-sm"></span>
    }
    ?????
</button>
```

## ?? Status Codes

| Code | Status |
|------|--------|
| 1 | ? Delivered |
| 0, 3, 4 | ? Pending |
| 2, 5, 6, 8, 10, 11, 14 | ? Failed |

## ?? Switch Providers

### In appsettings.json
```json
{ "SMS": { "Provider": "FarazSMS" } }
```

### Or Environment Variable
```bash
SMS_PROVIDER=FarazSMS
```

### Or Runtime
```csharp
var smsIr = _factory.GetSmsService(SmsProvider.SmsIr);
var farazSms = _factory.GetSmsService(SmsProvider.FarazSMS);
```

## ?? Advanced Features (SMS.ir only)

| Feature | Method |
|---------|--------|
| Send Single | `SendSmsAsync()` |
| Send Bulk | `SendBulkAsync()` |
| Send Pattern | `SendVerifyAsync()` |
| Send OTP | `SendOtpAsync()` |
| Get Report | `GetReportAsync()` |
| Get Credit | `GetCreditAsync()` |
| Get Lines | `GetLinesAsync()` |
| Get Templates | `GetTemplatesAsync()` |
| Get Received | `GetReceivedMessagesAsync()` |
| Delete Scheduled | `DeleteScheduledAsync()` |

## ?? File Structure

```
Models/Services/SMS/
??? SmsIr.cs                  # SMS.ir DTOs
??? FarazSMS.cs              # FarazSMS DTOs

Services/SMS/
??? ISmsService.cs           # Common interface
??? SmsIrService.cs          # SMS.ir implementation
??? FarazSmsService.cs       # FarazSMS implementation
??? SmsServiceFactory.cs     # Provider factory
??? README.md                # Full documentation

Services/Utils/
??? SmsIr.cs                 # SMS.ir API client
??? SMS.cs                   # FarazSMS client (legacy)
??? README_SmsIr.md         # SMS.ir guide

Infrastructure/Http/
??? SmsIrHttpClientConfig.cs # HTTP client config
```

## ?? Troubleshooting

| Issue | Solution |
|-------|----------|
| API Key Not Found | Set `SMSIR_API_KEY` environment variable |
| Template Not Found | Set `SMSIR_OTP_TEMPLATE_ID` |
| HTTP Timeout | Increase timeout in config |
| DNS Issues | Already handled by `ShecanDnsHttpClientHandler` |

## ?? Full Documentation

See `Services/SMS/README.md` for complete documentation.

## ?? Resources

- [SMS.ir API Docs](https://docs.sms.ir/)
- [SMS.ir Panel](https://panel.sms.ir/)
- [Get API Key](https://panel.sms.ir/api-keys)

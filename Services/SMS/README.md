# SMS Service Integration Guide

## Overview

This application now supports **multiple SMS providers** with a unified interface:
- **SMS.ir** - Modern REST API with comprehensive features
- **FarazSMS** - Existing legacy provider

The architecture follows MVVM pattern with dependency injection and factory pattern for provider selection.

## Quick Start

### 1. Installation

Add the following packages to your project (if not already installed):

```bash
dotnet add package Microsoft.Extensions.Http
dotnet add package Newtonsoft.Json
dotnet add package RestSharp
```

### 2. Configuration (appsettings.json)

```json
{
  "SMS": {
    "Provider": "SmsIr"
  },
  "SmsIr": {
    "ApiKey": "your-sms-ir-api-key-here",
    "Templates": {
      "OTP": "123456",
      "Welcome": "789012",
      "DealClosed": "345678"
    }
  },
  "FarazSMS": {
    "BaseUrl": "https://ippanel.com/api/select",
    "User": "your-user",
    "Password": "your-password",
    "FromNumber": "3000505",
    "Patterns": {
      "Welcome": "hjdntm0kxrir9nb",
      "DealClosed": "sarlemrkderzb4c",
      "OTP": "rw4oh5fhij1ntvq"
    }
  }
}
```

### 3. Environment Variables (Production - Recommended)

```bash
# SMS Provider Selection
SMS_PROVIDER=SmsIr

# SMS.ir Configuration
SMSIR_API_KEY=your-api-key-here
SMSIR_OTP_TEMPLATE_ID=123456
SMSIR_WELCOME_TEMPLATE_ID=789012
SMSIR_DEAL_CLOSED_TEMPLATE_ID=345678

# FarazSMS Configuration (fallback)
FARAZSMS_BASEURL=https://ippanel.com/api/select
FARAZSMS_USER=your-user
FARAZSMS_PASSWORD=your-password
FARAZSMS_FROM_NUMBER=3000505
FARAZSMS_PATTERN_WELCOME=hjdntm0kxrir9nb
FARAZSMS_PATTERN_DEAL_CLOSED=sarlemrkderzb4c
FARAZSMS_PATTERN_OTP=rw4oh5fhij1ntvq
```

### 4. Dependency Injection Setup (Program.cs)

```csharp
using PicoPlus.Services.SMS;
using PicoPlus.Infrastructure.Http;

// Register SMS.ir HTTP client
builder.Services.AddHttpClient<SmsIr>(client =>
{
    client.BaseAddress = new Uri(SmsIrHttpClientConfig.BaseUrl);
    client.Timeout = TimeSpan.FromSeconds(SmsIrHttpClientConfig.TimeoutSeconds);
})
.ConfigurePrimaryHttpMessageHandler(() => new PicoPlus.Infrastructure.Http.ShecanDnsHttpClientHandler());

// Register FarazSMS (existing)
builder.Services.AddScoped<SMS.Send>();

// Register provider-specific services
builder.Services.AddScoped<SmsIrService>();
builder.Services.AddScoped<FarazSmsService>();

// Register factory and main service
builder.Services.AddScoped<SmsServiceFactory>();
builder.Services.AddScoped<ISmsService, SmsService>();

// Optional: Register direct access to SMS.ir for advanced features
builder.Services.AddScoped<SmsIr>();
```

## Usage Patterns

### Pattern 1: Using ISmsService (Recommended - Provider Agnostic)

This pattern automatically uses the configured provider (SmsIr or FarazSMS).

#### ViewModel

```csharp
using PicoPlus.Services.SMS;

public class AuthViewModel : ViewModelBase
{
    private readonly ISmsService _smsService;
    
    public AuthViewModel(ISmsService smsService)
    {
        _smsService = smsService;
        SendOtpCommand = new AsyncRelayCommand(SendOtpAsync);
    }
    
    private async Task SendOtpAsync()
    {
        try
        {
            await _smsService.SendOtpAsync(Phone, GeneratedOtpCode);
            // Success
        }
        catch (Exception ex)
        {
            ErrorMessage = $"??? ?? ????? ??: {ex.Message}";
        }
    }
}
```

#### View

```razor
@page "/auth/verify"
@inject AuthViewModel ViewModel

<EditForm Model="@ViewModel" OnValidSubmit="@HandleSendOtp">
    <InputText @bind-value="ViewModel.Phone" />
    <button type="submit">????? ?? ?????</button>
</EditForm>

@code {
    private async Task HandleSendOtp()
    {
        await ViewModel.SendOtpCommand.ExecuteAsync(null);
    }
}
```

### Pattern 2: Using Factory (Multiple Providers)

Use when you need to switch between providers dynamically.

```csharp
public class NotificationService
{
    private readonly SmsServiceFactory _smsFactory;
    
    public NotificationService(SmsServiceFactory smsFactory)
    {
        _smsFactory = smsFactory;
    }
    
    public async Task SendUrgentNotificationAsync(string mobile, string message)
    {
        // Try SMS.ir first
        try
        {
            var smsIr = _smsFactory.GetSmsService(SmsProvider.SmsIr);
            await smsIr.SendOtpAsync(mobile, message);
        }
        catch
        {
            // Fallback to FarazSMS
            var farazSms = _smsFactory.GetSmsService(SmsProvider.FarazSMS);
            await farazSms.SendOtpAsync(mobile, message);
        }
    }
}
```

### Pattern 3: Direct SMS.ir Access (Advanced Features)

Use when you need SMS.ir specific features like reports, credit check, etc.

```csharp
public class SmsReportViewModel : ViewModelBase
{
    private readonly SmsIr _smsIr;
    
    public SmsReportViewModel(SmsIr smsIr)
    {
        _smsIr = smsIr;
    }
    
    public async Task CheckCreditAsync()
    {
        var response = await _smsIr.GetCreditAsync();
        Credit = response.data?.credit ?? 0;
    }
    
    public async Task GetMessageStatusAsync(long messageId)
    {
        var report = await _smsIr.GetReportAsync(messageId);
        Status = Models.Services.SMS.SmsIr.StatusHelpers.GetStatusText(
            report.data?.status ?? 0
        );
    }
    
    public async Task<List<string>> GetAvailableLinesAsync()
    {
        var response = await _smsIr.GetLinesAsync();
        return response.data?.lines?
            .Where(l => l.isActive == true)
            .Select(l => l.lineNumber!)
            .ToList() ?? new List<string>();
    }
}
```

## Common Scenarios

### Scenario 1: User Registration

```csharp
public class RegisterViewModel : ViewModelBase
{
    private readonly ISmsService _smsService;
    
    public async Task CompleteRegistrationAsync()
    {
        try
        {
            // Register user in database
            var user = await _userService.CreateUserAsync(FirstName, LastName, Phone);
            
            // Send welcome SMS
            await _smsService.SendWelcomeAsync(
                Phone, 
                FirstName, 
                LastName, 
                user.Id
            );
            
            SuccessMessage = "??? ??? ?? ?????? ????? ??";
        }
        catch (Exception ex)
        {
            ErrorMessage = $"???: {ex.Message}";
        }
    }
}
```

### Scenario 2: OTP Verification

```csharp
public class OtpViewModel : ViewModelBase
{
    private readonly ISmsService _smsService;
    private string _generatedOtp;
    
    public async Task SendOtpAsync()
    {
        _generatedOtp = GenerateRandomOtp();
        
        await _smsService.SendOtpAsync(Phone, _generatedOtp);
        
        // Store in session/cache for verification
        await _sessionStorage.SetItemAsync("otp", _generatedOtp);
        await _sessionStorage.SetItemAsync("otp_expiry", DateTime.UtcNow.AddMinutes(5));
    }
    
    public async Task<bool> VerifyOtpAsync(string userEnteredOtp)
    {
        var storedOtp = await _sessionStorage.GetItemAsync<string>("otp");
        var expiry = await _sessionStorage.GetItemAsync<DateTime>("otp_expiry");
        
        if (DateTime.UtcNow > expiry)
        {
            ErrorMessage = "?? ????? ????? ??? ???";
            return false;
        }
        
        return userEnteredOtp == storedOtp;
    }
    
    private string GenerateRandomOtp()
    {
        return Random.Shared.Next(100000, 999999).ToString();
    }
}
```

### Scenario 3: Deal Notification

```csharp
public class DealViewModel : ViewModelBase
{
    private readonly ISmsService _smsService;
    
    public async Task CloseDealAsync()
    {
        try
        {
            // Update deal status
            var deal = await _dealService.CloseDealAsync(DealId);
            
            // Send SMS notification
            await _smsService.SendDealClosedAsync(
                deal.CustomerPhone,
                deal.CustomerFirstName,
                deal.CustomerLastName,
                deal.Id
            );
            
            SuccessMessage = "?????? ?? ?????? ???? ??";
        }
        catch (Exception ex)
        {
            ErrorMessage = $"???: {ex.Message}";
        }
    }
}
```

### Scenario 4: Bulk SMS (SMS.ir only)

```csharp
public class BulkSmsViewModel : ViewModelBase
{
    private readonly SmsIr _smsIr;
    
    public async Task SendBulkSmsAsync(List<string> phoneNumbers, string message)
    {
        try
        {
            var lineNumber = await _smsIr.GetDefaultLineNumberAsync();
            
            var request = new Models.Services.SMS.SmsIr.SendBulk.Request
            {
                lineNumber = lineNumber,
                messageText = message,
                mobiles = phoneNumbers
            };
            
            var response = await _smsIr.SendBulkAsync(request);
            
            if (response.status == 200)
            {
                SuccessMessage = $"{response.data?.count} ???? ????? ??. ?????: {response.data?.cost:N0} ????";
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = $"???: {ex.Message}";
        }
    }
}
```

### Scenario 5: Message Tracking

```csharp
public class MessageTrackingViewModel : ViewModelBase
{
    private readonly SmsIr _smsIr;
    
    public async Task TrackMessageAsync(long messageId)
    {
        try
        {
            var report = await _smsIr.GetReportAsync(messageId);
            
            if (report.status == 200 && report.data != null)
            {
                MessageId = report.data.messageId;
                Mobile = report.data.mobile;
                StatusText = Models.Services.SMS.SmsIr.StatusHelpers.GetStatusText(
                    report.data.status ?? 0
                );
                Cost = report.data.cost;
                IsDelivered = Models.Services.SMS.SmsIr.StatusHelpers.IsSuccessful(
                    report.data.status ?? 0
                );
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = $"???: {ex.Message}";
        }
    }
}
```

## Provider Comparison

| Feature | SMS.ir | FarazSMS |
|---------|--------|----------|
| Send Single SMS | ? | ? |
| Send Bulk SMS | ? | ? |
| Pattern/Template SMS | ? | ? |
| OTP Support | ? | ? |
| Delivery Reports | ? | ? |
| Credit Check | ? | ? |
| Line Management | ? | ? |
| Receive SMS | ? | ? |
| Template List | ? | ? |
| Scheduled SMS | ? | ? |
| REST API | ? | ? |
| Modern Architecture | ? | ? |

## Switching Providers

### Option 1: Configuration File

Change `SMS:Provider` in appsettings.json:
```json
{
  "SMS": {
    "Provider": "SmsIr"  // or "FarazSMS"
  }
}
```

### Option 2: Environment Variable

```bash
SMS_PROVIDER=SmsIr
```

### Option 3: Runtime (Code)

```csharp
public class SmsConfigService
{
    private readonly SmsServiceFactory _factory;
    
    public void UseSmsIr()
    {
        var service = _factory.GetSmsService(SmsProvider.SmsIr);
        // Use service
    }
    
    public void UseFarazSms()
    {
        var service = _factory.GetSmsService(SmsProvider.FarazSMS);
        // Use service
    }
}
```

## Testing

### Unit Test Example

```csharp
[Fact]
public async Task SendOtp_Should_SendSuccessfully()
{
    // Arrange
    var mockConfig = new Mock<IConfiguration>();
    mockConfig.Setup(x => x["SmsIr:ApiKey"]).Returns("test-key");
    
    var mockHttp = new Mock<HttpClient>();
    var mockLogger = new Mock<ILogger<SmsIr>>();
    
    var smsIr = new SmsIr(mockHttp.Object, mockConfig.Object, mockLogger.Object);
    
    // Act
    await smsIr.SendOtpAsync("09123456789", "123456");
    
    // Assert
    // Verify HTTP call was made
}
```

## Troubleshooting

### Issue: API Key Not Found

**Error**: `InvalidOperationException: SMS.ir API key is not configured`

**Solution**: 
- Set `SMSIR_API_KEY` environment variable, or
- Add `SmsIr:ApiKey` to appsettings.json

### Issue: Template Not Found

**Error**: `InvalidOperationException: OTP template ID not configured`

**Solution**:
- Set `SMSIR_OTP_TEMPLATE_ID` environment variable, or
- Add `SmsIr:Templates:OTP` to appsettings.json

### Issue: HTTP Timeout

**Solution**: Increase timeout in `SmsIrHttpClientConfig`:
```csharp
public const int TimeoutSeconds = 60; // Increase from 30
```

### Issue: DNS Resolution (Iran)

The `ShecanDnsHttpClientHandler` is already configured for Iranian DNS issues.

## Best Practices

1. **Use ISmsService**: For provider-independent code
2. **Environment Variables**: For production secrets
3. **Error Handling**: Always wrap SMS calls in try-catch
4. **Logging**: Log all SMS operations for audit
5. **Rate Limiting**: Implement rate limiting to prevent abuse
6. **Cost Monitoring**: Regularly check credit balance
7. **Template Management**: Use templates for common messages
8. **Testing**: Use mock services in development

## Migration from FarazSMS to SMS.ir

1. Update configuration
2. No code changes needed (if using ISmsService)
3. Test all SMS flows
4. Monitor for errors
5. Keep FarazSMS as fallback initially

## Additional Resources

- [SMS.ir Documentation](https://docs.sms.ir/)
- [SMS.ir Dashboard](https://panel.sms.ir/)
- [FarazSMS Documentation](https://ippanel.com/)

## Support

For SMS.ir issues:
- Website: https://sms.ir
- Support: https://sms.ir/support

For integration issues:
- Check logs in Application Insights
- Review error messages
- Contact development team

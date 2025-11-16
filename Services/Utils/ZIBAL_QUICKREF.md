# ?? Zibal Quick Reference Guide

## Quick Start

### 1. Configuration
```bash
# .env file
ZIBAL_TOKEN=your_token_here
```

### 2. Inject Service
```csharp
public class YourService
{
    private readonly Zibal _zibalService;
    
    public YourService(Zibal zibalService)
    {
        _zibalService = zibalService;
    }
}
```

### 3. Use It!
```csharp
var response = await _zibalService.NationalIdentityInquiryAsync(request);
```

---

## ?? Available Methods (10 Total)

| Method | Purpose | Use Case |
|--------|---------|----------|
| `NationalIdentityInquiryAsync()` | Verify national ID + birthdate | KYC during registration |
| `ShahkarInquiryAsync()` | Verify mobile ownership | Mobile verification |
| `PostalCodeInquiryAsync()` | Get address from postal code | Address auto-fill |
| `IbanInquiryAsync()` | Verify IBAN account | Bank account validation |
| `BankAccountInquiryAsync()` | Verify bank account | Account ownership check |
| `CardNumberInquiryAsync()` | Verify card number | Card ownership check |
| `LicensePlateInquiryAsync()` | Get vehicle info | Vehicle verification |
| `BirthCertificateInquiryAsync()` | Verify birth certificate | Identity verification |
| `PhoneNumberInquiryAsync()` | Get landline subscriber | Landline verification |
| `CompanyInquiryAsync()` | Get company info | Business verification |
| `PassportInquiryAsync()` | Verify passport | Passport verification |

---

## ?? Common Use Cases

### ? Registration Flow (KYC)
```csharp
// 1. Verify identity
var identity = await _zibal.NationalIdentityInquiryAsync(new() 
{ 
    nationalCode = "1234567890", 
    birthDate = "1370/01/01" 
});

// 2. Verify mobile (Shahkar)
var shahkar = await _zibal.ShahkarInquiryAsync(new() 
{ 
    mobile = "09123456789", 
    nationalCode = "1234567890" 
});

// 3. Both must match
if (identity.data?.matched == true && shahkar.data?.matched == true)
{
    // Proceed with registration
}
```

### ?? Bank Account Verification
```csharp
var iban = await _zibal.IbanInquiryAsync(new() 
{ 
    iban = "IR123456789012345678901234" 
});

if (iban.data?.status == "active")
{
    // Account is valid
    var ownerName = iban.data.ownerName;
    var bankName = iban.data.bankName;
}
```

### ?? Vehicle Lookup
```csharp
var vehicle = await _zibal.LicensePlateInquiryAsync(new()
{
    plateChar1 = "12",
    plateChar2 = "???",
    plateChar3 = "345",
    plateChar4 = "10"
});

if (vehicle.data != null)
{
    Console.WriteLine($"{vehicle.data.model} - {vehicle.data.color}");
}
```

### ?? Address Auto-Fill
```csharp
// Get formatted address string
string address = await _zibal.GetPostalCodeAddressAsync("1234567890");

// Or get structured data
var postal = await _zibal.PostalCodeInquiryAsync(new() 
{ 
    postalCode = "1234567890" 
});
```

---

## ?? Error Handling

### Standard Pattern
```csharp
try
{
    var response = await _zibal.NationalIdentityInquiryAsync(request);
    
    if (response.result == 100) // Success code
    {
        if (response.data?.matched == true)
        {
            // Success!
        }
        else
        {
            // Data doesn't match
            ErrorMessage = "??????? ?????? ?????";
        }
    }
    else
    {
        // Failed with error message
        ErrorMessage = response.message ?? "???? ??????";
    }
}
catch (HttpRequestException ex)
{
    _logger.LogError(ex, "Network error");
    ErrorMessage = "??? ?? ?????? ?? ????";
}
catch (Exception ex)
{
    _logger.LogError(ex, "Unexpected error");
    ErrorMessage = "???? ?????????";
}
```

---

## ?? Response Codes

| Code | Meaning | Action |
|------|---------|--------|
| 100 | Success | Check `data.matched` |
| 102 | Invalid params | Validate inputs |
| 103 | Service down | Retry later |
| 104 | No credit | Contact Zibal |
| 401 | Bad token | Check configuration |

---

## ?? Best Practices

### ? DO
- ? Validate inputs before calling
- ? Log all requests for audit
- ? Cache results when appropriate
- ? Use async/await properly
- ? Handle all error cases
- ? Check `result == 100` before using data

### ? DON'T
- ? Expose API token to client
- ? Call APIs from client-side code
- ? Ignore error responses
- ? Skip null checks on `data`
- ? Call APIs in loops without rate limiting
- ? Store sensitive data in logs

---

## ?? Testing

### Mock for Unit Tests
```csharp
public class MockZibalService
{
    public Task<Zibal.NationalIdentityInquiry.Response> MockSuccess()
    {
        return Task.FromResult(new Zibal.NationalIdentityInquiry.Response
        {
            result = 100,
            message = "????",
            data = new() 
            { 
                matched = true, 
                firstName = "???", 
                lastName = "?????" 
            }
        });
    }
}
```

---

## ?? Support

- **Docs:** `Services/Utils/README_Zibal.md`
- **API Docs:** https://help.zibal.ir/facilities
- **Panel:** https://panel.zibal.ir/
- **Phone:** 021-91007053

---

## ?? Examples

See full examples in:
- `ViewModels/Auth/RegisterViewModel.cs` - Registration with KYC
- `Services/Utils/README_Zibal.md` - Complete documentation

---

**Last Updated:** 2025-01-15  
**Version:** 2.0

# Zibal Inquiry Services - Complete Documentation

## Overview
Complete implementation of Zibal inquiry APIs for identity verification, KYC compliance, and information lookup services.

**Official Documentation:** https://help.zibal.ir/facilities  
**Base URL:** `https://api.zibal.ir/v1/facility`  
**Authentication:** Bearer Token

## Configuration

### Environment Variable (Recommended)
```bash
ZIBAL_TOKEN=your_zibal_api_token_here
```

### appsettings.json (Alternative)
```json
{
  "Zibal": {
    "Token": "your_zibal_api_token_here"
  }
}
```

## Available Inquiry Services

### 1. ?? National Identity Inquiry (??????? ????)
**Purpose:** Verify Iranian national ID with birth date  
**Endpoint:** `/v1/facility/nationalIdentityInquiry`  
**Use Case:** KYC verification during registration

```csharp
var request = new Zibal.NationalIdentityInquiry.Request
{
    nationalCode = "1234567890",
    birthDate = "1370/01/01" // Persian date format
};

var response = await zibalService.NationalIdentityInquiryAsync(request);

if (response.data?.matched == true)
{
    var firstName = response.data.firstName;
    var lastName = response.data.lastName;
    var fatherName = response.data.fatherName;
    var isAlive = response.data.alive;
}
```

**Response Fields:**
- `matched` (bool): Whether information matches
- `firstName` (string): First name
- `lastName` (string): Last name
- `fatherName` (string): Father's name
- `alive` (bool): Life status

---

### 2. ?? Shahkar Inquiry (??????? ??????)
**Purpose:** Verify mobile number ownership with national code  
**Endpoint:** `/v1/facility/shahkarInquiry`  
**Use Case:** Mobile verification for KYC compliance

```csharp
var request = new Zibal.ShahkarInquiry.Request
{
    mobile = "09123456789",
    nationalCode = "1234567890"
};

var response = await zibalService.ShahkarInquiryAsync(request);

if (response.data?.matched == true)
{
    // Mobile number belongs to the national code
}
```

**Response Fields:**
- `matched` (bool): Whether mobile belongs to the national code

---

### 3. ?? Postal Code Inquiry (??????? ?? ????)
**Purpose:** Get full address details from 10-digit postal code  
**Endpoint:** `/v1/facility/postalCodeInquiry`  
**Use Case:** Address validation and auto-fill

```csharp
var request = new Zibal.PostalCodeInquiry.Request
{
    postalCode = "1234567890"
};

var response = await zibalService.PostalCodeInquiryAsync(request);

// Or get formatted address string
string address = await zibalService.GetPostalCodeAddressAsync("1234567890");
```

**Response Fields:**
- `province` (string): Province name
- `town` (string): City name
- `district` (string): District
- `street` (string): Main street
- `street2` (string): Secondary street
- `number` (int): Building number
- `floor` (string): Floor number
- `sideFloor` (string): Unit number
- `buildingName` (string): Building name

---

### 4. ?? IBAN Inquiry (??????? ???)
**Purpose:** Verify Iranian IBAN (Sheba) account number  
**Endpoint:** `/v1/facility/ibanInquiry`  
**Use Case:** Bank account verification for payments

```csharp
var request = new Zibal.IbanInquiry.Request
{
    iban = "IR123456789012345678901234" // 26 characters starting with IR
};

var response = await zibalService.IbanInquiryAsync(request);

if (response.data != null)
{
    var status = response.data.status;
    var bankName = response.data.bankName;
    var ownerName = response.data.ownerName;
    var accountNumber = response.data.accountNumber;
}
```

**Response Fields:**
- `status` (string): Account status (active/inactive)
- `bankName` (string): Bank name
- `ownerName` (string): Account owner name
- `accountNumber` (string): Account number

---

### 5. ?? Bank Account Inquiry (??????? ???? ?????)
**Purpose:** Verify bank account with national code  
**Endpoint:** `/v1/facility/bankAccountInquiry`  
**Use Case:** Verify account ownership

```csharp
var request = new Zibal.BankAccountInquiry.Request
{
    nationalCode = "1234567890",
    accountNumber = "1234567890",
    bankCode = "061" // Bank code (e.g., 061 for Saman Bank)
};

var response = await zibalService.BankAccountInquiryAsync(request);

if (response.data?.matched == true)
{
    var ownerName = response.data.ownerName;
    var status = response.data.status;
}
```

**Response Fields:**
- `matched` (bool): Whether account belongs to national code
- `ownerName` (string): Account owner name
- `status` (string): Account status

---

### 6. ?? Card Number Inquiry (??????? ????? ????)
**Purpose:** Verify 16-digit card number with national code  
**Endpoint:** `/v1/facility/cardNumberInquiry`  
**Use Case:** Card ownership verification

```csharp
var request = new Zibal.CardNumberInquiry.Request
{
    nationalCode = "1234567890",
    cardNumber = "1234567890123456" // 16 digits
};

var response = await zibalService.CardNumberInquiryAsync(request);

if (response.data?.matched == true)
{
    var bankName = response.data.bankName;
    var cardType = response.data.cardType;
}
```

**Response Fields:**
- `matched` (bool): Whether card belongs to national code
- `bankName` (string): Issuing bank name
- `cardType` (string): Card type (debit/credit)

---

### 7. ?? License Plate Inquiry (??????? ???? ?????)
**Purpose:** Get vehicle information by license plate  
**Endpoint:** `/v1/facility/licensePlateInquiry`  
**Use Case:** Vehicle verification

```csharp
var request = new Zibal.LicensePlateInquiry.Request
{
    plateChar1 = "12", // First two digits
    plateChar2 = "???", // Persian letter
    plateChar3 = "345", // Three digits
    plateChar4 = "10" // Iran number (two digits)
};

var response = await zibalService.LicensePlateInquiryAsync(request);

if (response.data != null)
{
    var model = response.data.model;
    var color = response.data.color;
    var year = response.data.year;
    var vehicleType = response.data.vehicleType;
    var ownerName = response.data.ownerName;
}
```

**Response Fields:**
- `model` (string): Vehicle model
- `color` (string): Vehicle color
- `year` (string): Manufacturing year
- `vehicleType` (string): Type of vehicle
- `ownerName` (string): Owner name

---

### 8. ?? Birth Certificate Inquiry (??????? ????????)
**Purpose:** Verify birth certificate information  
**Endpoint:** `/v1/facility/birthCertificateInquiry`  
**Use Case:** Identity verification

```csharp
var request = new Zibal.BirthCertificateInquiry.Request
{
    certificateNumber = "123456",
    nationalCode = "1234567890",
    birthDate = "1370/01/01" // Persian date
};

var response = await zibalService.BirthCertificateInquiryAsync(request);

if (response.data?.matched == true)
{
    var firstName = response.data.firstName;
    var lastName = response.data.lastName;
    var fatherName = response.data.fatherName;
    var birthPlace = response.data.birthPlace;
    var issuePlace = response.data.issuePlace;
}
```

**Response Fields:**
- `matched` (bool): Whether information matches
- `firstName` (string): First name
- `lastName` (string): Last name
- `fatherName` (string): Father's name
- `birthPlace` (string): Birth location
- `issuePlace` (string): Certificate issue location

---

### 9. ?? Phone Number Inquiry (??????? ???? ????)
**Purpose:** Get subscriber information by landline phone  
**Endpoint:** `/v1/facility/phoneNumberInquiry`  
**Use Case:** Landline verification

```csharp
var request = new Zibal.PhoneNumberInquiry.Request
{
    phoneNumber = "02112345678" // With area code
};

var response = await zibalService.PhoneNumberInquiryAsync(request);

if (response.data != null)
{
    var subscriberName = response.data.subscriberName;
    var address = response.data.address;
    var postalCode = response.data.postalCode;
}
```

**Response Fields:**
- `subscriberName` (string): Subscriber name
- `address` (string): Registered address
- `postalCode` (string): Postal code

---

### 10. ?? Company Inquiry (??????? ????)
**Purpose:** Get company information by national ID  
**Endpoint:** `/v1/facility/companyInquiry`  
**Use Case:** Business verification

```csharp
var request = new Zibal.CompanyInquiry.Request
{
    nationalId = "12345678901", // 11-digit company national ID
    registrationNumber = "123456" // Optional
};

var response = await zibalService.CompanyInquiryAsync(request);

if (response.data != null)
{
    var companyName = response.data.companyName;
    var companyType = response.data.companyType;
    var status = response.data.status;
    var registrationDate = response.data.registrationDate;
    var address = response.data.address;
    var postalCode = response.data.postalCode;
}
```

**Response Fields:**
- `companyName` (string): Company name
- `companyType` (string): Company type (LLC, etc.)
- `status` (string): Company status (active/inactive)
- `registrationDate` (string): Registration date
- `address` (string): Company address
- `postalCode` (string): Postal code

---

### 11. ?? Passport Inquiry (??????? ???????)
**Purpose:** Verify passport information  
**Endpoint:** `/v1/facility/passportInquiry`  
**Use Case:** Passport verification

```csharp
var request = new Zibal.PassportInquiry.Request
{
    passportNumber = "A12345678",
    nationalCode = "1234567890"
};

var response = await zibalService.PassportInquiryAsync(request);

if (response.data?.matched == true)
{
    var firstName = response.data.firstName;
    var lastName = response.data.lastName;
    var expiryDate = response.data.expiryDate;
    var issueDate = response.data.issueDate;
}
```

**Response Fields:**
- `matched` (bool): Whether passport belongs to national code
- `firstName` (string): First name
- `lastName` (string): Last name
- `expiryDate` (string): Expiry date
- `issueDate` (string): Issue date

---

## Response Codes

All endpoints return a `result` code:
- **100**: Success (????)
- **102**: Invalid parameters (?????????? ???????)
- **103**: Service unavailable (????? ?? ????? ????)
- **104**: Insufficient credit (?????? ???? ????)
- **401**: Unauthorized (???? ???????)

## Error Handling

All methods include comprehensive error handling and logging:

```csharp
try
{
    var response = await zibalService.NationalIdentityInquiryAsync(request);
    
    if (response.result == 100 && response.data?.matched == true)
    {
        // Success
    }
    else
    {
        // Failed - check response.message
        _logger.LogWarning("Zibal inquiry failed: {Message}", response.message);
    }
}
catch (HttpRequestException ex)
{
    // Network error
    _logger.LogError(ex, "Network error calling Zibal");
}
catch (Exception ex)
{
    // Other error
    _logger.LogError(ex, "Error calling Zibal");
}
```

## Usage in Registration Flow

Example: Complete KYC verification during user registration

```csharp
// Step 1: Verify national identity
var identityRequest = new Zibal.NationalIdentityInquiry.Request
{
    nationalCode = nationalCode,
    birthDate = persianBirthDate
};
var identityResponse = await _zibalService.NationalIdentityInquiryAsync(identityRequest);

if (identityResponse.data?.matched != true)
{
    throw new ValidationException("??????? ????? ????? ???");
}

// Step 2: Verify mobile ownership (Shahkar)
var shahkarRequest = new Zibal.ShahkarInquiry.Request
{
    mobile = phoneNumber,
    nationalCode = nationalCode
};
var shahkarResponse = await _zibalService.ShahkarInquiryAsync(shahkarRequest);

if (shahkarResponse.data?.matched != true)
{
    throw new ValidationException("????? ?????? ????? ?? ?? ??? ????");
}

// Step 3: Get address from postal code (optional)
var addressRequest = new Zibal.PostalCodeInquiry.Request
{
    postalCode = postalCode
};
var addressResponse = await _zibalService.PostalCodeInquiryAsync(addressRequest);

// Proceed with registration...
```

## Testing

For development/testing, you can use Zibal's test credentials (if available) or mock the service:

```csharp
// Mock for testing
public class MockZibalService : Zibal
{
    public override async Task<NationalIdentityInquiry.Response> NationalIdentityInquiryAsync(
        NationalIdentityInquiry.Request request)
    {
        return new NationalIdentityInquiry.Response
        {
            result = 100,
            message = "????",
            data = new NationalIdentityInquiry.Response.Data
            {
                matched = true,
                firstName = "???",
                lastName = "?????",
                fatherName = "???",
                alive = true
            }
        };
    }
}
```

## Rate Limiting

Be aware of Zibal's rate limits:
- Typical limit: 10-100 requests per minute (varies by subscription)
- Implement caching for frequently queried data
- Use circuit breaker pattern for resilience

## Security Best Practices

1. **Never expose the API token** in client-side code
2. **Store tokens in environment variables** or secure configuration
3. **Log all inquiry requests** for audit trails
4. **Implement rate limiting** on your API endpoints
5. **Validate all inputs** before sending to Zibal
6. **Use HTTPS** for all communications
7. **Implement proper error handling** to avoid leaking sensitive information

## Support

- **Official Documentation:** https://help.zibal.ir/facilities
- **Support:** https://panel.zibal.ir/ (Support section)
- **Phone:** 021-91007053

---

## Changelog

### Version 2.0 (Current)
- ? Added 10 inquiry endpoints
- ? Comprehensive error handling
- ? Full XML documentation
- ? Logging support
- ? Backward compatibility
- ? Persian/English documentation

### Version 1.0
- Basic implementation (3 endpoints)
- National Identity, Shahkar, Postal Code inquiries

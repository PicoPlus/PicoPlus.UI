# Shahkar Status Field Fix - Numeric Codes

## Problem
The `shahkar_status` field in HubSpot Contact was being set with string values like `"verified"`, `"not_matched"`, `"error"` instead of **numeric status codes** as required by the field type.

---

## Root Cause

In `RegisterViewModel.cs`, the `VerifyPhoneWithShahkarAsync` method was setting string labels:

**Before:**
```csharp
if (shahkarResponse?.result == 100 && shahkarResponse.data?.matched == true)
{
    ShahkarStatus = "verified";  // ? String label
}
else
{
    ShahkarStatus = "not_matched";  // ? String label  
}
```

**HubSpot field type**: `number` (numeric field)
**Code was sending**: `"verified"`, `"not_matched"`, `"error"` (string labels)

---

## Solution Implemented

Changed `shahkar_status` to use **numeric status codes** stored as strings (for JSON compatibility):

### Numeric Status Codes

| Code | Meaning | Description |
|------|---------|-------------|
| **0** | Not Checked | Shahkar verification was not performed |
| **100** | Verified | Phone number verified and matched with national code |
| **101** | Not Matched | Phone number exists but doesn't match national code |
| **500** | Error | Error occurred during Shahkar verification |
| **999** | Unknown | Unknown or unexpected response from Shahkar |

---

## Code Changes

### File: `ViewModels/Auth/RegisterViewModel.cs`

#### 1. Updated `VerifyPhoneWithShahkarAsync` Method

**Before:**
```csharp
if (shahkarResponse?.result == 100 && shahkarResponse.data?.matched == true)
{
    PhoneVerified = true;
    ShahkarStatus = "verified";  // ? String label
}
else
{
    PhoneVerified = false;
    ShahkarStatus = "not_matched";  // ? String label
}
```

**After:**
```csharp
if (shahkarResponse?.result == 100 && shahkarResponse.data?.matched == true)
{
    PhoneVerified = true;
    ShahkarStatus = "100";  // ? Numeric code: Verified
    _logger.LogInformation("Shahkar verification successful - Status: 100 (Verified)");
}
else if (shahkarResponse?.result == 100 && shahkarResponse.data?.matched == false)
{
    PhoneVerified = false;
    ShahkarStatus = "101";  // ? Numeric code: Not Matched
    _logger.LogWarning("Shahkar verification failed: Phone not matched - Status: 101");
}
else
{
    PhoneVerified = false;
    ShahkarStatus = shahkarResponse?.result?.ToString() ?? "999";  // ? Actual Zibal code or unknown
}
```

**Error Handling:**
```csharp
catch (Exception ex)
{
    _logger.LogError(ex, "Error during Shahkar verification");
    PhoneVerified = false;
    ShahkarStatus = "500";  // ? Numeric code: Error
}
```

#### 2. Updated `RegisterAsync` Method

**Before:**
```csharp
shahkar_status = ShahkarStatus ?? "verified"  // ? Default to string
```

**After:**
```csharp
shahkar_status = ShahkarStatus ?? "0"  // ? Default to 0 (Not Checked)
```

**Added Logging:**
```csharp
_logger.LogInformation("Contact created successfully: {ContactId}, Shahkar Status: {ShahkarStatus}", 
    contact.id, ShahkarStatus ?? "0");
```

---

## Shahkar Verification Flow

```
OTP Verified ?
   ?
Call Zibal ShahkarInquiryAsync
   ?
???????????????????????????????????
? Zibal Shahkar API Response      ?
???????????????????????????????????
   ?
Result == 100 && Matched == true?
   ?? YES ? Status = "100" (Verified) ?
   ?? Result == 100 && Matched == false?
   ?     ?? YES ? Status = "101" (Not Matched) ??
   ?? Other ? Status = Actual Result or "999" (Unknown) ?
   
Exception?
   ?? Status = "500" (Error) ?
   
Not Called?
   ?? Status = "0" (Not Checked) ?
```

---

## HubSpot Integration

### Contact Properties Sent

```json
{
  "properties": {
    "email": "0923889698@picoplus.app",
    "natcode": "0923889698",
    "firstname": "????",
    "lastname": "?????",
    "dateofbirth": "1370/01/15",
    "father_name": "???",
    "phone": "09937391536",
    "isverifiedbycr": "true",
    "shahkar_status": "100"  ? Numeric code (as string)
  }
}
```

### HubSpot Field Configuration

**Field Name**: `shahkar_status`  
**Field Type**: `number`  
**Possible Values**:
- `0` = Not checked
- `100` = Verified
- `101` = Not matched
- `500` = Error
- `999` = Unknown
- Other = Zibal API result code

---

## Status Code Reference

### 0 - Not Checked
**When**: Shahkar verification was skipped or not performed  
**Action**: Can be checked later if needed  
**User Impact**: None - registration proceeds

### 100 - Verified ?
**When**: Phone number matched with national code via Shahkar  
**Meaning**: Highest trust level - phone ownership confirmed  
**User Impact**: Full access, trusted user

### 101 - Not Matched ??
**When**: Phone number exists but belongs to different national code  
**Meaning**: Phone number is valid but doesn't match user's ID  
**User Impact**: May require additional verification  
**Note**: User can still register (OTP was verified)

### 500 - Error ?
**When**: Exception occurred during Shahkar API call  
**Possible Causes**:
- Network timeout
- API credentials issue
- Zibal service unavailable
**User Impact**: Registration continues, can retry later

### 999 - Unknown ?
**When**: Unexpected response from Zibal API  
**Meaning**: Could not determine match status  
**User Impact**: Manual review may be needed

---

## Testing

### Test Case 1: Successful Verification
1. Enter valid national code: `0923889698`
2. Enter matching phone: `09937391536`
3. Complete OTP verification
4. **Expected**: `shahkar_status = "100"`

### Test Case 2: Not Matched
1. Enter national code: `0923889698`
2. Enter phone belonging to different person: `09121234567`
3. Complete OTP verification
4. **Expected**: `shahkar_status = "101"`

### Test Case 3: Shahkar API Error
1. Temporarily disconnect network during Shahkar call
2. **Expected**: `shahkar_status = "500"`

### Test Case 4: Shahkar Not Called
1. Skip Shahkar verification (if optional)
2. **Expected**: `shahkar_status = "0"`

---

## Debugging

### Check Logs

**Successful Verification:**
```
Verifying phone with Shahkar: 09937391536, 0923889698
Shahkar response: Result=100, Matched=True
Shahkar verification successful - Status: 100 (Verified)
```

**Not Matched:**
```
Verifying phone with Shahkar: 09121234567, 0923889698
Shahkar response: Result=100, Matched=False
Shahkar verification failed: Phone not matched - Status: 101
```

**Error:**
```
Error during Shahkar verification
System.Net.Http.HttpRequestException: Connection timeout
Status set to: 500
```

### Verify in HubSpot

1. Go to HubSpot Contact record
2. Check `shahkar_status` property
3. Should show numeric value: `100`, `101`, `500`, etc.
4. **NOT** text values like "verified" or "error"

---

## Benefits of Numeric Codes

? **Type Safety**: Matches HubSpot field type (number)  
? **Standardization**: Consistent with API response codes  
? **Extensibility**: Easy to add new status codes  
? **Filtering**: Easier to query in HubSpot (status > 99)  
? **Logging**: Clear numeric codes in logs  
? **Internationalization**: Numbers don't need translation

---

## Migration Notes

### Existing Data

If you have existing contacts with old string values (`"verified"`, `"not_matched"`, `"error"`), you may want to:

1. **Option A: Leave as-is** - HubSpot may have converted them or set to null
2. **Option B: Data Migration** - Update old records:
   - `"verified"` ? `100`
   - `"not_matched"` ? `101`
   - `"error"` ? `500`

### HubSpot Workflow

If you have HubSpot workflows checking `shahkar_status`:

**Old Condition:**
```
shahkar_status is equal to "verified"
```

**New Condition:**
```
shahkar_status is equal to 100
```

---

## Summary

? **Fixed**: `shahkar_status` now uses numeric codes  
? **Codes**: 0=Not Checked, 100=Verified, 101=Not Matched, 500=Error, 999=Unknown  
? **Compatible**: Works with HubSpot number field type  
? **Logging**: Enhanced logging for debugging  
? **Default**: Falls back to "0" if Shahkar not checked  

The field is now properly aligned with HubSpot's number field type while maintaining full functionality! ??

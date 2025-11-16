# ?? Troubleshooting Guide - Properties Not Showing in Profile

## Issue
Contact properties (gender, father_name, shahkar_status, email, etc.) are not displaying in the user profile UI.

## ? FIXED ISSUES

### 1. Navigation Route Mismatch ?
**Problem**: Login was navigating to `/user/panel` but the route is `/user`

**Files Fixed**:
- `ViewModels/Auth/LoginViewModel.cs` - Changed navigation from `/user/panel` to `/user`
- `Components/Pages/Home.razor` - Changed navigation from `/user/panel` to `/user`

### 2. Debug Logging Added ?
Added logging to `UserHomeViewModel.InitializeAsync()` to track which properties are loaded.

---

## ?? TESTING STEPS

### Step 1: Restart Application
```
Stop: Shift + F5
Start: F5
```

### Step 2: Login
1. Go to `/auth/login`
2. Enter national code: `0923889698`
3. Select role: **User**
4. Click **???? ?? ??????**

### Step 3: Check Debug Output
**View ? Output ? Select "Debug"**

Look for this log:
```
Contact loaded - ID: [id], Phone: [phone], Email: [email], Gender: [gender], FatherName: [father], ShahkarStatus: [status]
```

### Step 4: Check Profile Display
Navigate to profile tab and verify ALL fields are visible:

| Field | Should Show |
|-------|-------------|
| ??? (Name) | ? Always |
| ??? ???????? (Last Name) | ? Always |
| ?? ??? (National Code) | ? Always |
| ????? ?????? (Phone) | ? Always + Edit button |
| ????? ???? (Birth Date) | ? If not empty |
| ??? ??? (Father Name) | ? If not empty and not "-" |
| ????? (Gender) | ? If not empty and not "-" |
| ????? (Email) | ? Always |
| ????? ?????? (Shahkar) | ? Always with text |
| ????? ??? ????? (CR Verified) | ? Always |
| ??? ??? (Wallet) | ? Always formatted |
| ????? ??????? (Deal Count) | ? Always |
| ????? ???? (Total Revenue) | ? Always formatted |

---

## ?? DIAGNOSTIC CHECKLIST

### If Properties Still Not Showing:

#### 1. Check Session Storage
**Browser DevTools ? Application ? Session Storage**

Look for key: `ContactModel`

Value should be a JSON object with all properties:
```json
{
  "id": "...",
  "properties": {
    "firstname": "????",
    "lastname": "?????",
    "phone": "09937391536",
    "natcode": "0923889698",
    "email": "0923889698@picoplus.app",
    "gender": "???",
    "father_name": "???",
    "shahkar_status": "100",
    "dateofbirth": "1370/01/15",
    "wallet": "0",
    "total_revenue": "0",
    "isverifiedbycr": "true",
    "num_associated_deals": "2",
    "contact_plan": ""
  }
}
```

#### 2. Check HubSpot Data
1. Open HubSpot
2. Go to Contacts
3. Find contact with national code `0923889698`
4. Verify fields have values:
   - `gender`: should have value (e.g., "???")
   - `father_name`: should have value (e.g., "???")
   - `shahkar_status`: should have value (e.g., "100")
   - `email`: should have value

#### 3. Check LoginViewModel Fetch
**Debug Output** should show:
```
Attempting login for national code: 0923889698
Contact found: [contactId]. Checking for missing fields...
Checking missing fields for contact: [contactId]
Missing fields - FatherName: False, Gender: False, ShahkarStatus: False
Contact data refreshed and updated if needed: [contactId]
```

If it shows `True` for missing fields, the auto-update will run.

#### 4. Check ViewModel Properties
In `UserHomeViewModel`, the properties should return data:
```csharp
public string Gender => ContactModel?.properties?.gender ?? "-";
// Should NOT return "-" if gender exists in session
```

---

## ??? SOLUTIONS

### Solution 1: Clear Session and Re-login
```
Browser DevTools ? Application ? Session Storage ? Clear All
Refresh page ? Login again
```

### Solution 2: Force Re-fetch from HubSpot
If session storage has old data without new properties:

**Option A**: Clear session (see Solution 1)

**Option B**: Modify `UserHomeViewModel.InitializeAsync()` to always refresh:

```csharp
// After loading from session, refresh from HubSpot
var freshContact = await _contactService.Read(ContactModel.id, new[] {
    "firstname", "lastname", "email", "phone", "natcode",
    "dateofbirth", "father_name", "gender", "shahkar_status",
    "wallet", "total_revenue", "isverifiedbycr", "num_associated_deals"
});

// Map to Search.Response.Result format
ContactModel = new ContactModel.Search.Response.Result
{
    id = freshContact.id,
    properties = new ContactModel.Search.Response.Result.Properties
    {
        firstname = freshContact.properties.firstname,
        lastname = freshContact.properties.lastname,
        // ... map all properties
    }
};
```

### Solution 3: Verify Model Inheritance
Check that `Contact.Search.Response.Result.Properties` inherits from `Contact.Read.Response.Properties`:

```csharp
// In Contact.Dto.cs
public class Result
{
    public string id { get; set; }
    public Properties properties { get; set; }
    // ...
    
    public class Properties : Read.Response.Properties { }
    // ? Should inherit all properties from Base.Response.Properties
}
```

---

## ?? EXPECTED RESULTS

### After Login:
1. ? Redirected to `/user`
2. ? Statistics cards show numbers
3. ? Profile tab shows ALL 13 fields
4. ? Conditional fields (gender, father_name) appear if data exists
5. ? Shahkar status shows readable text with emoji
6. ? Edit button appears next to phone number

### Debug Logs Should Show:
```
info: PicoPlus.ViewModels.Auth.LoginViewModel[0]
      Attempting login for national code: 0923889698
info: PicoPlus.ViewModels.Auth.LoginViewModel[0]
      Contact found: 123456789. Checking for missing fields...
info: PicoPlus.Services.CRM.ContactUpdateService[0]
      Checking missing fields for contact: 123456789
info: PicoPlus.ViewModels.Auth.LoginViewModel[0]
      Contact data refreshed and updated if needed: 123456789
info: PicoPlus.ViewModels.User.UserHomeViewModel[0]
      Contact loaded - ID: 123456789, Phone: 09937391536, Email: 0923889698@picoplus.app, Gender: ???, FatherName: ???, ShahkarStatus: 100
info: PicoPlus.ViewModels.User.UserHomeViewModel[0]
      User panel initialized for: 123456789
```

---

## ?? QUICK TEST

Run this in Browser Console to check session data:
```javascript
// Check if session storage has contact data
const contactData = sessionStorage.getItem('ContactModel');
console.log(JSON.parse(contactData));

// Should show all properties including:
// - gender
// - father_name
// - shahkar_status
// - email
// etc.
```

---

## ?? NEED HELP?

If properties still don't show after following all steps:

1. **Share debug logs** from Output window
2. **Share session storage** JSON (remove sensitive data)
3. **Share HubSpot contact** field values
4. **Share screenshot** of profile tab

The issue is likely one of:
- Session storage has old data (clear and re-login)
- HubSpot contact doesn't have values (check HubSpot)
- Navigation going to wrong route (check URL is /user)
- Model inheritance broken (check Contact.Dto.cs)

---

**Most Common Cause**: Old session data without new properties. **Quick Fix**: Clear session storage and login again!

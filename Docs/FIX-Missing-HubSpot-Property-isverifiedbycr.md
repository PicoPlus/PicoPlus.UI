# ??? FIX: Create Missing HubSpot Custom Property `isverifiedbycr`

## ? ERROR

```json
{
  "status": "error",
  "message": "Property values were not valid",
  "error": "PROPERTY_DOESNT_EXIST",
  "name": "isverifiedbycr"
}
```

The custom property `isverifiedbycr` doesn't exist in your HubSpot portal (Portal ID: 20890764).

---

## ? SOLUTION 1: Create the Property in HubSpot (RECOMMENDED)

### Step 1: Login to HubSpot
1. Go to: https://app.hubspot.com/
2. Login to your account (Portal ID: 20890764)

### Step 2: Navigate to Properties Settings
1. Click on the **?? Settings** icon (top right)
2. In the left sidebar, go to: **Data Management** ? **Properties**
3. Select object type: **Contact properties**

### Step 3: Create Custom Property
Click **"Create property"** button and fill in:

| Field | Value |
|-------|-------|
| **Object type** | Contact |
| **Group** | Contact Information |
| **Label** | Is Verified By CR |
| **Description** | Indicates if contact is verified by Civil Registration (??? ?????) through Zibal API |
| **Field type** | Single checkbox |
| **Internal name** | `isverifiedbycr` |

**OR** if you want to store "true"/"false" as text:

| Field | Value |
|-------|-------|
| **Object type** | Contact |
| **Group** | Contact Information |
| **Label** | Is Verified By CR |
| **Description** | Indicates if contact is verified by Civil Registration (??? ?????) |
| **Field type** | Single-line text |
| **Internal name** | `isverifiedbycr` |

### Step 4: Save the Property
1. Click **"Create"** or **"Save"**
2. The property is now available for use

---

## ? SOLUTION 2: Remove Property Temporarily (QUICK FIX)

If you want to test without creating the property, I can remove all references to `isverifiedbycr` from the code.

### Files That Need Changes:
1. `Services/CRM/Objects/Contact.cs` - Remove from default properties list
2. `ViewModels/User/UserHomeViewModel.cs` - Remove from update calls
3. `ViewModels/Auth/RegisterViewModel.cs` - Remove from registration
4. `Models/CRM/Objects/Contact.Dto.cs` - Comment out property definition
5. Any read/write operations that include this property

---

## ?? PROPERTY DETAILS

### What is `isverifiedbycr`?

**Purpose**: Tracks whether a contact has been verified through Zibal's National Identity Inquiry API against Iran's Civil Registration (??? ?????).

**Set To `"true"` When**:
- User completes birth date in the dialog
- Zibal API returns successful verification (`result == 1`, `matched == true`)
- System successfully retrieves: father_name, gender, firstname, lastname

**Used In**:
1. **Registration** (`ViewModels/Auth/RegisterViewModel.cs`):
   - Set during national identity verification step
   
2. **Birth Date Completion** (`ViewModels/User/UserHomeViewModel.cs`):
   - Set when user provides missing birth date
   - Zibal verifies the data

3. **User Profile Display** (`Views/User/Home.razor`):
   - Shows "????? ???" (Verified) or "????? ????" (Not Verified)
   - Displayed in "????? ???? ?? ??? ?????" field

### Property Values:
- `"true"` = Verified by Civil Registration ?
- `"false"` or `null` = Not verified ?

---

## ?? WHICH SOLUTION TO CHOOSE?

### Choose Solution 1 (Create Property) IF:
- ? You have HubSpot admin access
- ? You want full functionality
- ? You want to track CR verification status
- ? You're deploying to production

### Choose Solution 2 (Remove Property) IF:
- ? You want to test quickly
- ? You don't have HubSpot admin access right now
- ? CR verification status is not critical for your current testing
- ? You'll create the property later

---

## ?? LET ME KNOW WHICH SOLUTION

**Reply with:**
- **"1"** or **"create property"** ? I'll wait for you to create it in HubSpot, then we can test
- **"2"** or **"remove property"** ? I'll remove all references to this property from the code

OR if you've already created the property in HubSpot, just say **"done"** or **"created"** and we can test!

---

## ?? ADDITIONAL CUSTOM PROPERTIES YOU MIGHT NEED

While you're in HubSpot Properties, you may also want to verify these custom properties exist:

| Property Name | Type | Purpose |
|---------------|------|---------|
| `isverifiedbycr` | Single checkbox or Text | Verified by Civil Registration |
| `father_name` | Single-line text | Father's name (from Zibal) |
| `dateofbirth` | Single-line text | Birth date (Persian format) |
| `natcode` | Single-line text | National code (?? ???) |
| `shahkar_status` | Single-line text | Shahkar verification status code |
| `gender` | Single-line text | Gender (from Zibal) |
| `wallet` | Number | Wallet balance |
| `contact_plan` | Single-line text | Contact plan/tier |

These are all used by your application. If any are missing, you'll get similar errors.

---

## ? QUICK CHECK: Which Properties Exist?

To check which properties already exist in your portal, you can call the HubSpot Properties API:

```bash
curl -X GET \
  "https://api.hubapi.com/crm/v3/properties/contacts" \
  -H "authorization: Bearer YOUR_HUBSPOT_TOKEN"
```

Or just try creating the `isverifiedbycr` property and see if it works!

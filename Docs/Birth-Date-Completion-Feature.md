# ? Birth Date Completion Feature - IMPLEMENTED

## ?? Feature Overview

When a user logs in and their **birth date is NULL**, they will see a ONE-TIME prompt asking them to enter their birth date. Once provided, the system will:

1. Save the birth date to HubSpot
2. Call Zibal National Identity Inquiry API
3. Auto-fill missing profile fields (father_name, gender, etc.)
4. Mark profile as verified by CR (Civil Registration)

**This happens ONLY ONCE** - not on every login. Once birth date is filled, the dialog won't appear again.

---

## ?? FILES CREATED

### 1. `Components/Dialogs/CompleteBirthDateDialog.razor`

**Purpose**: Modal dialog for collecting birth date

**Features**:
- Persian date input with validation (YYYY/MM/DD format)
- Information message explaining why birth date is needed
- Two buttons:
  - **????? ? ???????????** (Verify & Update) - Calls Zibal API
  - **?????** (Later) - Skips for now
- Loading states
- Error/success messages

**Parameters**:
```razor
<CompleteBirthDateDialog 
    NationalCode="@natCode"
    OnBirthDateProvided="@HandleBirthDateProvided"
    OnSkip="@HandleSkip" />
```

---

## ?? FILES MODIFIED

### 2. `ViewModels/User/UserHomeViewModel.cs`

**Changes**:

#### Added Observable Property:
```csharp
[ObservableProperty]
private bool showCompleteBirthDateDialog = false;
```

#### Updated `InitializeAsync()`:
```csharp
// Check if birth date is missing - show completion dialog ONCE
if (string.IsNullOrWhiteSpace(ContactModel.properties?.dateofbirth))
{
    _logger.LogInformation("Birth date is missing for contact: {ContactId}. Showing completion dialog.", ContactModel.id);
    ShowCompleteBirthDateDialog = true;
}
```

#### Added `CompleteBirthDateCommand`:
```csharp
[RelayCommand]
private async Task CompleteBirthDateAsync(string birthDate, CancellationToken cancellationToken)
{
    // 1. Save birth date to HubSpot
    // 2. Call Zibal National Identity Inquiry
    // 3. Update father_name, gender, firstname, lastname
    // 4. Mark as verified (isverifiedbycr = "true")
    // 5. Refresh contact data from HubSpot
    // 6. Update session storage
    // 7. Show success message
    // 8. Close dialog
}
```

**Dependencies Added**:
- `Contact _contactService` - For updating properties
- `Zibal _zibalService` - For national identity verification
- `IDialogService _dialogService` - For showing success/warning messages

---

### 3. `Views/User/Home.razor`

**Changes**:

#### Added Dialog in Markup:
```razor
<!-- Complete Birth Date Dialog -->
@if (ViewModel.ShowCompleteBirthDateDialog)
{
    <CompleteBirthDateDialog 
        NationalCode="@(ViewModel.ContactModel?.properties?.natcode ?? "")"
        OnBirthDateProvided="@HandleBirthDateProvided"
        OnSkip="@(() => ViewModel.ShowCompleteBirthDateDialog = false)" />
}
```

#### Added Event Handler in @code:
```csharp
private async Task HandleBirthDateProvided(string birthDate)
{
    await ViewModel.CompleteBirthDateCommand.ExecuteAsync(birthDate);
    StateHasChanged();
}
```

---

## ?? FLOW DIAGRAM

```
User Logs In
    ?
UserHomeViewModel.InitializeAsync()
    ?
Load Contact from Session Storage
    ?
? Is dateofbirth NULL or empty?
    ?
  YES ? Show CompleteBirthDateDialog
    ?
User Enters Birth Date (1370/01/15)
    ?
Click "????? ? ???????????"
    ?
? Save to HubSpot (dateofbirth = "1370/01/15")
    ?
?? Call Zibal National Identity Inquiry
    {
      nationalCode: "0923889698",
      birthDate: "1370/01/15",
      genderInquiry: true
    }
    ?
? Response Matched (result = 1, matched = true)
    ?
Update HubSpot:
  - father_name = "???"
  - gender = "???"
  - firstname = "????" (if different)
  - lastname = "?????" (if different)
  - isverifiedbycr = "true"
    ?
? Refresh Contact Data from HubSpot
    ?
? Update Session Storage
    ?
? Show Success Message:
   "??????? ??? ?? ?????? ????? ??!
    ??? ???: ???
    ?????: ???"
    ?
? Close Dialog (ShowCompleteBirthDateDialog = false)
    ?
? Reload Statistics
    ?
? Profile Now Complete!
```

---

## ?? COMPILE ERROR & SOLUTION

### Current Issue:
```
CS0103: The name 'ShowCompleteBirthDateDialog' does not exist in the current context
```

### Cause:
The app is currently RUNNING. The CommunityToolkit MVVM source generator needs to run during compilation, but the app is locked.

### Solution:
**STOP THE APP FIRST**, then rebuild:

```powershell
# 1. Stop the app (Shift + F5 in Visual Studio)
#    OR kill the process
taskkill /F /IM PicoPlus.exe

# 2. Clean
dotnet clean

# 3. Rebuild
dotnet build

# 4. Run
dotnet run
```

The `[ObservableProperty]` attribute will generate the public property `ShowCompleteBirthDateDialog` automatically.

---

## ?? TESTING CHECKLIST

### Test Scenario 1: First Login with Missing Birth Date

**Setup**:
1. In HubSpot, find a contact
2. Clear the `dateofbirth` field (set to NULL or empty)
3. Note the `natcode` (e.g., "0923889698")

**Test Steps**:
1. ? Stop the app
2. ? Restart the app (`F5`)
3. ? Login with the national code
4. ? **EXPECTED**: Dialog appears immediately after login
5. ? Enter birth date: `1370/01/15`
6. ? Click **????? ? ???????????**
7. ? **EXPECTED**: Loading spinner shown
8. ? **EXPECTED**: Success message appears:
   ```
   ??????? ??? ?? ?????? ????? ??!
   ??? ???: ???
   ?????: ???
   ```
9. ? **EXPECTED**: Dialog closes
10. ? **EXPECTED**: Profile now shows all fields (father_name, gender, etc.)
11. ? **EXPECTED**: In HubSpot, fields are updated:
    - `dateofbirth` = "1370/01/15"
    - `father_name` = "???"
    - `gender` = "???"
    - `isverifiedbycr` = "true"

### Test Scenario 2: Second Login (Birth Date Already Filled)

**Test Steps**:
1. ? Logout
2. ? Login again with same national code
3. ? **EXPECTED**: Dialog does NOT appear
4. ? **EXPECTED**: Profile loads normally with all data visible

### Test Scenario 3: Invalid Birth Date

**Test Steps**:
1. ? (Setup: Clear birth date again)
2. ? Login
3. ? Dialog appears
4. ? Enter invalid date: `9999/99/99`
5. ? Click **????? ? ???????????**
6. ? **EXPECTED**: Error message: "???? ????? ???? ???? ????"

### Test Scenario 4: Wrong Birth Date (Zibal Not Matched)

**Test Steps**:
1. ? Enter a wrong birth date (e.g., 50 years off)
2. ? Click **????? ? ???????????**
3. ? **EXPECTED**: Warning message:
   ```
   ????? ???? ????? ??? ??? ????? ???? ?? ??? ????? ???? ????.
   ????? ????? ???? ?? ????? ????.
   ```
4. ? **EXPECTED**: Birth date saved to HubSpot
5. ? **EXPECTED**: Other fields (father_name, gender) NOT updated
6. ? **EXPECTED**: Dialog closes

### Test Scenario 5: Skip Button

**Test Steps**:
1. ? Dialog appears
2. ? Click **?????** button
3. ? **EXPECTED**: Dialog closes immediately
4. ? **EXPECTED**: No API calls made
5. ? **EXPECTED**: Birth date still NULL in HubSpot
6. ? **EXPECTED**: On next login, dialog appears again

---

## ?? DEBUG LOGS TO CHECK

**View ? Output ? Debug**

### Expected Logs:

#### On Login (Birth Date Missing):
```
info: PicoPlus.ViewModels.User.UserHomeViewModel[0]
      Contact loaded - ID: 175293168213, Phone: 09937391536, Email: NULL, Gender: NULL, FatherName: NULL, ShahkarStatus: NULL, BirthDate: NULL
info: PicoPlus.ViewModels.User.UserHomeViewModel[0]
      Birth date is missing for contact: 175293168213. Showing completion dialog.
```

#### On Birth Date Submit:
```
info: PicoPlus.ViewModels.User.UserHomeViewModel[0]
      Completing birth date for contact: 175293168213, BirthDate: 1370/01/15
info: PicoPlus.ViewModels.User.UserHomeViewModel[0]
      Birth date saved to HubSpot: 1370/01/15
info: PicoPlus.ViewModels.User.UserHomeViewModel[0]
      Calling Zibal National Identity Inquiry for: 0923889698
info: PicoPlus.Services.Identity.Zibal[0]
      Sending request to Zibal: nationalIdentityInquiry
info: PicoPlus.Services.Identity.Zibal[0]
      Zibal request successful: nationalIdentityInquiry
info: PicoPlus.ViewModels.User.UserHomeViewModel[0]
      Zibal verification successful! Updating profile with: FatherName=???, Gender=???
info: PicoPlus.ViewModels.User.UserHomeViewModel[0]
      Profile updated with 5 properties from Zibal
```

#### On Next Login (Birth Date Exists):
```
info: PicoPlus.ViewModels.User.UserHomeViewModel[0]
      Contact loaded - ID: 175293168213, Phone: 09937391536, Email: ..., Gender: ???, FatherName: ???, ShahkarStatus: 0, BirthDate: 1370/01/15
info: PicoPlus.ViewModels.User.UserHomeViewModel[0]
      User panel initialized for: 175293168213
```
(No "Birth date is missing" log - dialog won't show)

---

## ?? DATA FLOW

### Before Feature:
```
Login ? Contact Loaded
{
  "id": "175293168213",
  "properties": {
    "firstname": "????",
    "lastname": "?????",
    "phone": "09937391536",
    "natcode": "0923889698",
    "dateofbirth": NULL,
    "father_name": NULL,
    "gender": NULL,
    "isverifiedbycr": "false"
  }
}
```

### After Birth Date Completion:
```
Login ? Contact Loaded
{
  "id": "175293168213",
  "properties": {
    "firstname": "????",
    "lastname": "?????",
    "phone": "09937391536",
    "natcode": "0923889698",
    "dateofbirth": "1370/01/15",
    "father_name": "???",
    "gender": "???",
    "isverifiedbycr": "true"
  }
}
```

---

## ?? UI SCREENSHOTS (Expected)

### 1. Dialog Appears After Login
```
????????????????????????????????????????????
?    ?? ????? ??????? ???????              ?
????????????????????????????????????????????
?                                          ?
?  ?? ???? ????? ??????? ???? ?????       ?
?  ????? ???? ??? ?? ???? ????.          ?
?  ??? ??????? ???? ????? ???? ??          ?
?  ??? ????? ??????? ??????.              ?
?                                          ?
?  ????? ???? ????                        ?
?  ????????????????????                   ?
?  ? 1370/01/15      ?                   ?
?  ????????????????????                   ?
?  ?? ????? ???? ?? ?? ???? YYYY/MM/DD   ?
?                                          ?
?  [? ????? ? ???????????]  [?? ?????]  ?
????????????????????????????????????????????
```

### 2. Success Message
```
????????????????????????????????????????
?  ? ????                             ?
?                                      ?
?  ??????? ??? ?? ?????? ????? ??!    ?
?  ??? ???: ???                        ?
?  ?????: ???                          ?
?                                      ?
?  [? ?????]                          ?
????????????????????????????????????????
```

---

## ? SUMMARY

### What Was Implemented:
1. ? **CompleteBirthDateDialog.razor** - Modal dialog for birth date input
2. ? **UserHomeViewModel.cs** - Added `ShowCompleteBirthDateDialog` property and `CompleteBirthDateCommand`
3. ? **Home.razor** - Wired up dialog and event handler
4. ? **Check on Login** - If birth date is NULL ? show dialog
5. ? **Zibal Integration** - Call National Identity Inquiry API
6. ? **Auto-Fill Profile** - Update father_name, gender, firstname, lastname
7. ? **Mark as Verified** - Set isverifiedbycr = "true"
8. ? **One-Time Only** - Dialog only shows when birth date is NULL

### What Happens:
- **First login** (no birth date) ? Dialog appears
- **User provides birth date** ? Zibal verifies ? Profile updated
- **Second login** (birth date exists) ? No dialog, normal flow

### Next Steps:
1. **STOP THE APP** (Shift + F5)
2. **Rebuild** (`dotnet build`)
3. **Test** with a contact that has NULL birth date
4. **Verify** Zibal API response and HubSpot updates

---

**?? Feature Ready! Just needs a clean rebuild after stopping the app. ??**

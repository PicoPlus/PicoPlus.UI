# Contact Auto-Update & Profile Enhancement - Implementation Guide

## ? COMPLETED FEATURES

### 1. ContactUpdateService (NEW SERVICE)
**File**: `Services/CRM/ContactUpdateService.cs`

**Features**:
- ? Automatically checks for missing/null fields on login
- ? Updates `father_name`, `gender`, `birthdate` from Zibal National Identity API
- ? Updates `shahkar_status` from Zibal Shahkar API
- ? Smart detection - only updates if fields are missing
- ? Comprehensive logging for debugging
- ? Error handling - doesn't block login if update fails

**How It Works**:
```csharp
// On every login, after finding contact:
contact = await _contactUpdateService.UpdateMissingFieldsAsync(contact, cancellationToken);

// Service checks these fields:
- father_name: null or empty?
- gender: null or empty?  
- shahkar_status: null, empty, or "0"?

// If missing, calls Zibal APIs:
1. National Identity Inquiry ? father_name, gender, first/last name
2. Shahkar Inquiry ? shahkar_status (100=verified, 101=not matched)
```

### 2. Login Auto-Update Integration
**File**: `ViewModels/Auth/LoginViewModel.cs`

**Changes**:
- ? Added `ContactUpdateService` dependency injection
- ? Added `gender` to properties fetch list
- ? Calls `UpdateMissingFieldsAsync()` after contact is found
- ? Updated contact stored in session with latest data

**Login Flow**:
```
User Enters National Code
    ?
Search HubSpot for Contact
    ?
? NEW: Auto-Update Missing Fields from Zibal
    ?
Store Updated Contact in Session
    ?
Navigate to User Panel
```

### 3. Contact Service Enhancement
**File**: `Services/CRM/Objects/Contact.cs`

**Added**:
- ? `UpdateContactProperties(contactId, Dictionary<string, string>)` method
- ? Allows updating multiple properties at once
- ? Uses HubSpot PATCH API

### 4. Contact Model Update
**File**: `Models/CRM/Objects/Contact.Dto.cs`

**Added**:
- ? `contact_plan` property (was missing)
- ? `gender` property (already existed)

### 5. Dependency Registration
**File**: `Program.cs`

**Added**:
- ? `builder.Services.AddScoped<PicoPlus.Services.CRM.ContactUpdateService>();`

---

## ?? TODO: REMAINING FEATURES

### Phase 2: Enhanced Profile Display

#### Task 1: Update UserHomeViewModel
**File**: `ViewModels/User/UserHomeViewModel.cs`

**Need to Add**:
```csharp
// Additional computed properties
public string Gender => ContactModel?.properties?.gender ?? "-";
public string ShahkarStatusText => GetShahkarStatusText(ContactModel?.properties?.shahkar_status);
public string EmailFormatted => ContactModel?.properties?.email ?? "-";
public string IsVerifiedByCR => ContactModel?.properties?.isverifiedbycr == "true" ? "??? ?" : "??? ?";

// Status helpers
private string GetShahkarStatusText(string? status)
{
    return status switch
    {
        "100" => "????? ??? ?",
        "101" => "??? ????? ??",
        "500" => "??? ?? ????? ?",
        "0" => "????? ???? ?",
        _ => "??????"
    };
}

public string GetGenderText(string? gender)
{
    return gender switch
    {
        "???" => "??? ??",
        "??" => "?? ??",
        _ => "-"
    };
}
```

#### Task 2: Update User Home View - Show All Properties
**File**: `Views/User/Home.razor`

**Need to Add** (in Profile Tab section):

```razor
<!-- Existing fields: firstname, lastname, natcode, phone, dateofbirth, father_name -->

<!-- ADD THESE NEW FIELDS -->

<div class="col-md-6">
    <label class="dark-label">
        <i class="bi bi-gender-ambiguous me-2"></i>?????
    </label>
    <input type="text" class="dark-input persian-text" 
           value="@ViewModel.GetGenderText(ViewModel.Gender)" 
           readonly />
</div>

<div class="col-md-6">
    <label class="dark-label">
        <i class="bi bi-envelope me-2"></i>?????
    </label>
    <input type="text" class="dark-input" 
           value="@ViewModel.EmailFormatted" 
           readonly />
</div>

<div class="col-md-6">
    <label class="dark-label">
        <i class="bi bi-shield-check me-2"></i>????? ??????
    </label>
    <input type="text" class="dark-input persian-text" 
           value="@ViewModel.ShahkarStatusText" 
           readonly />
</div>

<div class="col-md-6">
    <label class="dark-label">
        <i class="bi bi-patch-check me-2"></i>????? ???? ?? ??? ?????
    </label>
    <input type="text" class="dark-input persian-text" 
           value="@ViewModel.IsVerifiedByCR" 
           readonly />
</div>

<div class="col-md-6">
    <label class="dark-label">
        <i class="bi bi-wallet2 me-2"></i>?????? ??? ???
    </label>
    <input type="text" class="dark-input" 
           value="@ViewModel.FormatNumber(ViewModel.WalletBalance) ?????" 
           readonly />
</div>

<div class="col-md-6">
    <label class="dark-label">
        <i class="bi bi-briefcase me-2"></i>????? ???????
    </label>
    <input type="text" class="dark-input" 
           value="@ViewModel.TotalDeals" 
           readonly />
</div>
```

---

### Phase 3: Mobile Number Change with OTP

#### Task 3: Create ChangeMobileDialog Component
**File**: `Components/Dialogs/ChangeMobileDialog.razor` (NEW)

```razor
@using Microsoft.AspNetCore.Components.Forms
@using PicoPlus.Services.Auth
@using PicoPlus.Services.SMS
@inject OtpService OtpService
@inject ISmsService SmsService

<div class="modal-overlay" @onclick="CloseDialog">
    <div class="modal-dark change-mobile-dialog" @onclick:stopPropagation="true">
        <div class="modal-dark-header">
            <h5 class="modal-dark-title">
                <i class="bi bi-phone me-2"></i>
                ????? ????? ??????
            </h5>
            <button type="button" class="btn-close-dark" @onclick="CloseDialog">
                <i class="bi bi-x-lg"></i>
            </button>
        </div>

        <div class="modal-dark-body" dir="rtl">
            @if (!otpSent)
            {
                <!-- Step 1: Enter New Mobile -->
                <div class="mb-4">
                    <label class="dark-label">????? ?????? ????</label>
                    <input type="text" 
                           class="dark-input" 
                           @bind="newMobile"
                           placeholder="09123456789"
                           maxlength="11" />
                    <small class="text-muted d-block mt-2">
                        <i class="bi bi-info-circle me-1"></i>
                        ????? ?????? ???? ?? 09 ???? ???
                    </small>
                </div>

                @if (!string.IsNullOrEmpty(errorMessage))
                {
                    <div class="alert alert-danger">
                        <i class="bi bi-exclamation-triangle me-2"></i>@errorMessage
                    </div>
                }
            }
            else
            {
                <!-- Step 2: Enter OTP -->
                <div class="text-center mb-4">
                    <i class="bi bi-shield-lock text-info" style="font-size: 3rem;"></i>
                    <p class="mt-3">
                        ?? ????? ?? ????? <strong>@newMobile</strong> ????? ??
                    </p>
                </div>

                <div class="mb-4">
                    <label class="dark-label">?? ?????</label>
                    <input type="text" 
                           class="dark-input text-center" 
                           @bind="otpCode"
                           placeholder="?? 6 ????"
                           maxlength="6"
                           style="font-size: 1.5rem; letter-spacing: 0.5rem;" />
                </div>

                @if (!string.IsNullOrEmpty(otpRemainingTime))
                {
                    <div class="text-center mb-3">
                        <small class="text-muted">
                            <i class="bi bi-clock me-1"></i>
                            ???? ???? ?????: <strong>@otpRemainingTime</strong>
                        </small>
                    </div>
                }

                @if (!string.IsNullOrEmpty(errorMessage))
                {
                    <div class="alert alert-danger">
                        <i class="bi bi-exclamation-triangle me-2"></i>@errorMessage
                    </div>
                }
            }
        </div>

        <div class="modal-dark-footer">
            @if (!otpSent)
            {
                <button type="button" 
                        class="btn btn-primary" 
                        @onclick="SendOtp"
                        disabled="@isLoading">
                    @if (isLoading)
                    {
                        <span class="spinner-border spinner-border-sm me-2"></span>
                    }
                    <i class="bi bi-send me-2"></i>????? ?? ?????
                </button>
            }
            else
            {
                <button type="button" 
                        class="btn btn-link" 
                        @onclick="SendOtp"
                        disabled="@(!canResendOtp || isLoading)">
                    <i class="bi bi-arrow-repeat me-2"></i>????? ????
                </button>

                <button type="button" 
                        class="btn btn-success" 
                        @onclick="VerifyOtpAndUpdate"
                        disabled="@isLoading">
                    @if (isLoading)
                    {
                        <span class="spinner-border spinner-border-sm me-2"></span>
                    }
                    <i class="bi bi-check-circle me-2"></i>????? ? ?????
                </button>
            }

            <button type="button" class="btn btn-secondary" @onclick="CloseDialog">
                <i class="bi bi-x-lg me-2"></i>??????
            </button>
        </div>
    </div>
</div>

@code {
    [Parameter] public string CurrentMobile { get; set; } = string.Empty;
    [Parameter] public EventCallback<string> OnMobileChanged { get; set; }
    [Parameter] public EventCallback OnCancel { get; set; }

    private string newMobile = string.Empty;
    private string otpCode = string.Empty;
    private string errorMessage = string.Empty;
    private string otpRemainingTime = string.Empty;
    private bool otpSent = false;
    private bool canResendOtp = false;
    private bool isLoading = false;
    private System.Timers.Timer? otpTimer;

    protected override void OnInitialized()
    {
        newMobile = CurrentMobile;
    }

    private async Task SendOtp()
    {
        errorMessage = string.Empty;

        if (string.IsNullOrWhiteSpace(newMobile) || !newMobile.StartsWith("09") || newMobile.Length != 11)
        {
            errorMessage = "????? ?????? ????? ????";
            return;
        }

        isLoading = true;
        try
        {
            // Generate and store OTP
            var otp = OtpService.GenerateOtp();
            OtpService.StoreOtp(newMobile, otp);

            // Send OTP via SMS
            await SmsService.SendOtpAsync(newMobile, otp);

            otpSent = true;
            canResendOtp = false;

            // Start countdown
            StartOtpTimer();
        }
        catch (Exception ex)
        {
            errorMessage = $"??? ?? ????? ??: {ex.Message}";
        }
        finally
        {
            isLoading = false;
        }
    }

    private async Task VerifyOtpAndUpdate()
    {
        errorMessage = string.Empty;

        if (string.IsNullOrWhiteSpace(otpCode))
        {
            errorMessage = "????? ?? ????? ?? ???? ????";
            return;
        }

        isLoading = true;
        try
        {
            var result = OtpService.ValidateOtp(newMobile, otpCode);

            if (result.IsValid)
            {
                // OTP verified - call parent callback
                await OnMobileChanged.InvokeAsync(newMobile);
            }
            else
            {
                errorMessage = result.ErrorMessage;
            }
        }
        finally
        {
            isLoading = false;
        }
    }

    private async Task CloseDialog()
    {
        otpTimer?.Dispose();
        await OnCancel.InvokeAsync();
    }

    private void StartOtpTimer()
    {
        otpTimer?.Dispose();
        otpTimer = new System.Timers.Timer(1000);
        otpTimer.Elapsed += (s, e) => UpdateOtpTimer();
        otpTimer.Start();
    }

    private void UpdateOtpTimer()
    {
        var remaining = OtpService.GetRemainingTime(newMobile);
        if (remaining.HasValue && remaining.Value.TotalSeconds > 0)
        {
            otpRemainingTime = $"{remaining.Value.Minutes:D2}:{remaining.Value.Seconds:D2}";
            InvokeAsync(StateHasChanged);
        }
        else
        {
            otpRemainingTime = string.Empty;
            canResendOtp = true;
            otpTimer?.Stop();
            InvokeAsync(StateHasChanged);
        }
    }

    public void Dispose()
    {
        otpTimer?.Dispose();
    }
}
```

#### Task 4: Add Mobile Change to UserHomeViewModel
**File**: `ViewModels/User/UserHomeViewModel.cs`

```csharp
// Add these properties and methods:

[ObservableProperty]
private bool showChangeMobileDialog = false;

[RelayCommand]
private async Task ChangeMobileAsync(string newMobile, CancellationToken cancellationToken)
{
    try
    {
        IsLoading = true;

        // Update contact phone in HubSpot
        var properties = new Dictionary<string, string>
        {
            ["phone"] = newMobile
        };

        await _contactService.UpdateContactProperties(ContactModel!.id, properties);

        // Verify new phone with Shahkar
        var shahkarResponse = await _zibalService.ShahkarInquiryAsync(
            new Models.Services.Identity.Zibal.ShahkarInquiry.Request
            {
                mobile = newMobile,
                nationalCode = ContactModel.properties.natcode
            });

        string shahkarStatus = "0";
        if (shahkarResponse?.result == 100 && shahkarResponse.data?.matched == true)
        {
            shahkarStatus = "100";
        }
        else if (shahkarResponse?.result == 100 && shahkarResponse.data?.matched == false)
        {
            shahkarStatus = "101";
        }

        // Update Shahkar status
        properties["shahkar_status"] = shahkarStatus;
        await _contactService.UpdateContactProperties(ContactModel.id, properties);

        // Refresh contact data
        await InitializeAsync(cancellationToken);

        await _dialogService.ShowSuccessAsync(
            "????",
            "????? ?????? ?? ?????? ????? ???");

        ShowChangeMobileDialog = false;
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Error changing mobile number");
        await _dialogService.ShowErrorAsync(
            "???",
            "??? ?? ????? ????? ??????");
    }
    finally
    {
        IsLoading = false;
    }
}
```

#### Task 5: Add Change Mobile Button to User Home View
**File**: `Views/User/Home.razor`

```razor
<!-- In Profile Tab, add button next to phone field -->

<div class="col-md-6">
    <label class="dark-label">
        <i class="bi bi-phone me-2"></i>????? ??????
    </label>
    <div class="d-flex gap-2">
        <input type="text" class="dark-input flex-grow-1" 
               value="@(ViewModel.ContactModel?.properties?.phone ?? "-")" 
               readonly />
        <button type="button" 
                class="btn btn-outline-info" 
                @onclick="() => ViewModel.ShowChangeMobileDialog = true">
            <i class="bi bi-pencil"></i>
        </button>
    </div>
</div>

<!-- Add dialog at bottom of file -->
@if (ViewModel.ShowChangeMobileDialog)
{
    <ChangeMobileDialog 
        CurrentMobile="@(ViewModel.ContactModel?.properties?.phone ?? "")"
        OnMobileChanged="@(async (newMobile) => await ViewModel.ChangeMobileCommand.ExecuteAsync(newMobile))"
        OnCancel="@(() => ViewModel.ShowChangeMobileDialog = false)" />
}
```

---

## ?? TESTING CHECKLIST

### Auto-Update on Login
- [ ] Login with contact missing `father_name`
- [ ] Verify father_name gets updated from Zibal
- [ ] Login with contact missing `gender`
- [ ] Verify gender gets updated
- [ ] Login with `shahkar_status = 0`
- [ ] Verify Shahkar status gets updated to 100 or 101
- [ ] Check logs for "Checking missing fields for contact"

### Profile Display
- [ ] All fields visible: name, lastname, natcode, phone, birthdate, father_name, gender
- [ ] Shahkar status shows readable text (????? ???, ??? ?????, etc.)
- [ ] CR verification shows checkmark or X
- [ ] Wallet balance formatted correctly
- [ ] Deal count matches statistics card

### Mobile Change
- [ ] Click edit button next to phone number
- [ ] Dialog opens
- [ ] Enter new mobile ? OTP sent
- [ ] Enter correct OTP ? Mobile updated
- [ ] Shahkar verified for new mobile
- [ ] Profile refreshes with new mobile
- [ ] Enter wrong OTP ? Error shown
- [ ] Resend OTP works after timeout

---

## ?? KEY BENEFITS

? **No Manual Updates**: Fields auto-fill from Zibal on every login  
? **Always Current**: Shahkar status refreshed if missing  
? **Complete Profile**: All HubSpot fields visible to user  
? **Secure Mobile Change**: OTP verification required  
? **Smart Updates**: Only updates missing fields, doesn't overwrite existing data  
? **Error Handling**: Login continues even if Zibal update fails  

---

## ?? DEPLOYMENT NOTES

1. **Environment Variables Required**:
   - `ZIBAL_TOKEN` - For identity verification
   - `HUBSPOT_TOKEN` - For contact updates
   - `SMSIR_API_KEY` - For OTP sending

2. **HubSpot Fields Required**:
   - `father_name` (text)
   - `gender` (text)
   - `shahkar_status` (number)
   - `contact_plan` (text)

3. **Testing Accounts**:
   - Create test contacts with missing fields
   - Test with real Zibal API (sandbox if available)

---

## ?? DOCUMENTATION CREATED

- ? `Services/CRM/ContactUpdateService.cs` - Full inline documentation
- ? This implementation guide
- ? Auto-update logic flowcharts (in comments)
- ? Status code reference (Shahkar: 0, 100, 101, 500, 999)

---

## ?? NEXT STEPS

1. **Complete Profile Display** (30 min):
   - Add helper methods to UserHomeViewModel
   - Update User Home view with all fields
   - Test display

2. **Mobile Change Dialog** (1 hour):
   - Create ChangeMobileDialog component
   - Add to UserHomeViewModel
   - Wire up in User Home view
   - Test OTP flow

3. **Final Testing** (30 min):
   - Test auto-update on login
   - Test profile display
   - Test mobile change
   - Check logs

**Total Remaining Time**: ~2 hours

---

**Your system now has auto-update on login! The remaining work is mostly UI enhancements. Great progress! ??**

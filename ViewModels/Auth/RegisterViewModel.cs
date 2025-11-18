using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PicoPlus.Infrastructure.Services;
using PicoPlus.Infrastructure.State;
using PicoPlus.Models.CRM.Objects;
using Microsoft.Extensions.Logging;
using System.Globalization;
using ContactService = PicoPlus.Services.CRM.Objects.Contact;
using ZibalService = PicoPlus.Services.Identity.Zibal;
using SmsIrService = PicoPlus.Services.SMS.SmsIrService;
using OtpService = PicoPlus.Services.Auth.OtpService;
using ShahkarInquiry = PicoPlus.Services.Identity.Zibal;
using ImageProcessingService = PicoPlus.Services.Imaging.ImageProcessingService;

namespace PicoPlus.ViewModels.Auth;

/// <summary>
/// ViewModel for Register page with complete registration flow
/// Flow: NationalCode → BirthDate → Zibal Verification → Phone → OTP → HubSpot Creation → Avatar Upload
/// </summary>
public partial class RegisterViewModel : BaseViewModel
{
    private readonly ContactService _contactService;
    private readonly ZibalService _zibalService;
    private readonly SmsIrService _smsService;
    private readonly OtpService _otpService;
    private readonly ImageProcessingService _imageProcessingService;
    private readonly INavigationService _navigationService;
    private readonly IDialogService _dialogService;
    private readonly ISessionStorageService _sessionStorage;
    private readonly AuthenticationStateService _authState;
    private readonly ILogger<RegisterViewModel> _logger;

    // Step 1: National Code (from login or manual entry)
    [ObservableProperty]
    private string nationalCode = string.Empty;

    // Step 2: Birth Date (Persian format: YYYY/MM/DD)
    [ObservableProperty]
    private string birthDate = string.Empty;

    // Step 3: Zibal Identity Verification Results
    [ObservableProperty]
    private string firstName = string.Empty;

    [ObservableProperty]
    private string lastName = string.Empty;

    [ObservableProperty]
    private string fatherName = string.Empty;

    [ObservableProperty]
    private string gender = string.Empty;

    [ObservableProperty]
    private bool? alive;

    [ObservableProperty]
    private bool isVerified;

    // Step 4: Phone Number
    [ObservableProperty]
    private string phone = string.Empty;

    // Step 5: OTP Verification
    [ObservableProperty]
    private string otpCode = string.Empty;

    [ObservableProperty]
    private bool otpSent;

    [ObservableProperty]
    private string otpRemainingTime = string.Empty;

    [ObservableProperty]
    private bool canResendOtp = true;

    // Step 6: Shahkar (Mobile Verification)
    [ObservableProperty]
    private bool phoneVerified;

    [ObservableProperty]
    private string? shahkarStatus;

    // Current registration step
    [ObservableProperty]
    private int currentStep = 1;

    private System.Timers.Timer? _otpTimer;

    public RegisterViewModel(
        ContactService contactService,
        ZibalService zibalService,
        SmsIrService smsService,
        OtpService otpService,
        ImageProcessingService imageProcessingService,
        INavigationService navigationService,
        IDialogService dialogService,
        ISessionStorageService sessionStorage,
        AuthenticationStateService authState,
        ILogger<RegisterViewModel> logger)
    {
        _contactService = contactService;
        _zibalService = zibalService;
        _smsService = smsService;
        _otpService = otpService;
        _imageProcessingService = imageProcessingService;
        _navigationService = navigationService;
        _dialogService = dialogService;
        _sessionStorage = sessionStorage;
        _authState = authState;
        _logger = logger;
        Title = "ثبت نام در سامانه";
    }

    /// <summary>
    /// Initialize - check if redirected from login with pending national code
    /// </summary>
    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        var pendingNatCode = await _sessionStorage.GetItemAsync<string>("PendingNationalCode", cancellationToken);
        if (!string.IsNullOrEmpty(pendingNatCode))
        {
            NationalCode = pendingNatCode;
            await _sessionStorage.RemoveItemAsync("PendingNationalCode", cancellationToken);
            _logger.LogInformation("Initialized registration with national code from login: {NationalCode}", NationalCode);
        }
    }

    /// <summary>
    /// Step 2: Verify national identity with Zibal
    /// </summary>
    [RelayCommand]
    private async Task VerifyNationalIdentityAsync(CancellationToken cancellationToken)
    {
        await ExecuteAsync(async () =>
        {
            if (!ValidateNationalCodeAndBirthDate())
                return;

            _logger.LogInformation("Verifying national identity for: {NationalCode}", NationalCode);

            // BirthDate is already in Persian format (YYYY/MM/DD)
            var inquiry = await _zibalService.NationalIdentityInquiryAsync(new Models.Services.Identity.Zibal.NationalIdentityInquiry.Request
            {
                birthDate = BirthDate, // Already in correct format
                nationalCode = NationalCode,
                genderInquiry = true
            });

            _logger.LogInformation("Zibal inquiry result: {Result}, Matched: {Matched}",
                inquiry?.result,
                inquiry?.data?.matched);

            if (inquiry?.result == 1 && inquiry.data?.matched == true)
            {
                FirstName = inquiry.data.firstName ?? string.Empty;
                LastName = inquiry.data.lastName ?? string.Empty;
                FatherName = inquiry.data.fatherName ?? string.Empty;

                Gender = inquiry.data.gender ?? string.Empty;
                Alive = inquiry.data.alive;
                IsVerified = true;
                CurrentStep = 2;

                _logger.LogInformation("National identity verified successfully for: {FirstName} {LastName}, CurrentStep: {CurrentStep}",
                    FirstName, LastName, CurrentStep);

                // Clear any previous errors
                HasError = false;
                ErrorMessage = string.Empty;

                // Show success notification
                await _dialogService.ShowSuccessAsync(
                    "????? ???? ????",
                    $"???? ??? ?? ?????? ????? ??: {FirstName} {LastName}");
            }
            else
            {
                _logger.LogWarning("Zibal verification failed - Result: {Result}, Matched: {Matched}, Message: {Message}",
                    inquiry?.result,
                    inquiry?.data?.matched,
                    inquiry?.message);

                await _dialogService.ShowErrorAsync(
                    "??? ?? ??????? ????",
                    inquiry?.message ?? "??????? ???? ??? ?? ????? ??? ????? ?????? ?????. ????? ?? ??? ? ????? ???? ?? ????? ????.");

                // Reset for retry
                IsVerified = false;
                FirstName = string.Empty;
                LastName = string.Empty;
                FatherName = string.Empty;
                Gender = string.Empty;
                Alive = null;
            }
        }, cancellationToken);
    }

    /// <summary>
    /// Step 4: Send OTP to phone number
    /// </summary>
    [RelayCommand]
    private async Task SendOtpAsync(CancellationToken cancellationToken)
    {
        await ExecuteAsync(async () =>
        {
            if (!ValidatePhoneNumber())
                return;

            _logger.LogInformation("Sending OTP to phone: {Phone}", Phone);

            // Generate OTP
            var otp = _otpService.GenerateOtp();
            
            // Store OTP with rate limiting
            if (!_otpService.StoreOtp(Phone, otp, out var rateLimitError))
            {
                ErrorMessage = rateLimitError;
                HasError = true;
                return;
            }

            // Send OTP via SMS
            await _smsService.SendOtpAsync(Phone, otp);

            OtpSent = true;
            CurrentStep = 3;
            CanResendOtp = false;

            // Start countdown timer
            StartOtpTimer();

            _logger.LogInformation("OTP sent successfully to: {Phone}", Phone);
        }, cancellationToken);
    }

    /// <summary>
    /// Step 5: Verify OTP code
    /// </summary>
    [RelayCommand]
    private async Task VerifyOtpAsync(CancellationToken cancellationToken)
    {
        await ExecuteAsync(async () =>
        {
            if (string.IsNullOrWhiteSpace(OtpCode))
            {
                ErrorMessage = "????? ?? ????? ?? ???? ????";
                HasError = true;
                return;
            }

            _logger.LogInformation("Verifying OTP for phone: {Phone}, Entered Code: {Code}", Phone, OtpCode);

            var result = _otpService.ValidateOtp(Phone, OtpCode);

            _logger.LogInformation("OTP validation result: IsValid={IsValid}, ErrorMessage={ErrorMessage}",
                result.IsValid, result.ErrorMessage);

            if (result.IsValid)
            {
                _logger.LogInformation("OTP verified successfully for: {Phone}", Phone);

                // Show success message
                await _dialogService.ShowSuccessAsync(
                    "????? ????",
                    "?? ????? ?? ?????? ????? ??!");

                // Verify phone with Shahkar (optional but recommended)
                await VerifyPhoneWithShahkarAsync(cancellationToken);

                // Proceed to final registration
                CurrentStep = 4;

                // Clear any previous errors
                HasError = false;
                ErrorMessage = string.Empty;

                _logger.LogInformation("Advanced to Step 4 - Ready for final registration");
            }
            else
            {
                ErrorMessage = result.ErrorMessage;
                HasError = true;
                _logger.LogWarning("OTP verification failed for: {Phone}, Error: {Error}", Phone, result.ErrorMessage);

                // Show error toast
                await _dialogService.ShowErrorAsync(
                    "?? ????? ??????",
                    result.ErrorMessage);
            }
        }, cancellationToken);
    }

    /// <summary>
    /// Step 6: Complete registration and create HubSpot contact
    /// </summary>
    [RelayCommand]
    private async Task RegisterAsync(CancellationToken cancellationToken)
    {
        await ExecuteAsync(async () =>
        {
            if (!ValidateFinalData())
                return;

            _logger.LogInformation("Creating HubSpot contact for: {NationalCode}", NationalCode);

            // Create contact in HubSpot with all collected data
            var contact = await _contactService.Create(new Contact.Create.Request
            {
                properties = new Contact.Create.Request.Properties
                {
                    email = $"{NationalCode}@picoplus.app",
                    natcode = NationalCode,
                    firstname = FirstName,
                    lastname = LastName,
                    dateofbirth = BirthDate,
                    father_name = FatherName,
                    phone = Phone,
                    gender = Gender, // Already "1" or "2" from Zibal

                    // Use numeric status code: 100=verified, 101=not_matched, 500=error, 0=not_checked
                    shahkar_status = ShahkarStatus ?? "0"
                }
            });

            if (!string.IsNullOrEmpty(contact.id))
            {
                _logger.LogInformation("Contact created successfully: {ContactId}, Gender: {Gender}, Shahkar Status: {ShahkarStatus}",
                    contact.id, Gender, ShahkarStatus ?? "0");

                // Temporarily disabled - national card image upload until Zibal endpoint is available
                try
                {
                    await UploadNationalCardImageAsync(contact.id, cancellationToken);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to upload national card image for contact: {ContactId}", contact.id);
                    // Don't fail registration if image upload fails
                }


                // Convert to search result format for state management
                var userModel = new Contact.Search.Response.Result
                {
                    id = contact.id,
                    properties = new Contact.Search.Response.Result.Properties
                    {
                        email = contact.properties.email,
                        firstname = contact.properties.firstname,
                        lastname = contact.properties.lastname,
                        phone = contact.properties.phone,
                        natcode = contact.properties.natcode,
                        dateofbirth = contact.properties.dateofbirth,
                        father_name = contact.properties.father_name,
                        total_revenue = contact.properties.total_revenue,
                        shahkar_status = contact.properties.shahkar_status,
                        wallet = contact.properties.wallet,
                        gender = contact.properties.gender,
                        num_associated_deals = contact.properties.num_associated_deals,
                        contact_plan = contact.properties.contact_plan
                    },
                    createdAt = contact.createdAt.ToString("o"),
                    updatedAt = contact.updatedAt.ToString("o"),
                    archived = contact.archived
                };

                // Set authentication state
                _authState.SetAuthenticatedUser(userModel);
                await _sessionStorage.SetItemAsync("LogInState", 1, cancellationToken);
                await _sessionStorage.SetItemAsync("ContactModel", userModel, cancellationToken);

                // Send welcome SMS
                await SendWelcomeSmsAsync(cancellationToken);

                // Show success message
                await _dialogService.ShowSuccessAsync(
                    "ثبت نام موفق",
                    $"خوش آمدید {FirstName} {LastName}! حساب کاربری شما با موفقیت ایجاد شد.");

                // Navigate to dashboard
                _navigationService.NavigateTo("/user");
            }
            else
            {
                await _dialogService.ShowErrorAsync("خطا", "خطا در ایجاد حساب کاربری");
            }
        }, cancellationToken);
    }

    /// <summary>
    /// Upload national card image from Zibal to HubSpot
    /// </summary>
    private async Task UploadNationalCardImageAsync(string contactId, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Fetching national card image from Zibal for contact: {ContactId}", contactId);

            // TODO: Enable when Zibal nationalCardImageInquiry endpoint is available
            // Currently this endpoint returns HTML instead of JSON, indicating it may not be available yet
            _logger.LogWarning("National card image inquiry is currently disabled - endpoint not available from Zibal");
            return;

            // Fetch national card image from Zibal
            var imageResponse = await _zibalService.NationalCardImageInquiryAsync(
                new Models.Services.Identity.Zibal.NationalCardImageInquiry.Request
                {
                    nationalCode = NationalCode,
                    birthDate = BirthDate
                });

            if (imageResponse?.result == 1 &&
                imageResponse.data?.matched == true &&
                !string.IsNullOrWhiteSpace(imageResponse.data.nationalCardImage))
            {
                _logger.LogInformation("National card image fetched successfully, converting to JPG");

                // Convert base64 to optimized JPG (quality 85, max 1200px width)
                var jpgBytes = await _imageProcessingService.ConvertBase64ToOptimizedJpgAsync(
                    imageResponse.data.nationalCardImage,
                    quality: 85,
                    maxWidth: 1200
                );

                _logger.LogInformation("Image converted to JPG ({Size}KB), uploading to HubSpot", jpgBytes.Length / 1024);

                // Upload to HubSpot
                var avatarUrl = await _contactService.UploadAvatarAsync(
                    contactId,
                    jpgBytes,
                    $"avatar_{NationalCode}.jpg"
                );

                if (!string.IsNullOrEmpty(avatarUrl))
                {
                    _logger.LogInformation("Avatar uploaded successfully: {Url}", avatarUrl);
                }
                else
                {
                    _logger.LogWarning("Avatar upload returned null URL");
                }
            }
            else
            {
                _logger.LogWarning("Zibal national card image inquiry failed or returned no image. Result: {Result}",
                    imageResponse?.result);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error uploading national card image");
            // Don't throw - image upload failure should not break registration
        }
    }

    /// <summary>
    /// Resend OTP code
    /// </summary>
    [RelayCommand]
    private async Task ResendOtpAsync(CancellationToken cancellationToken)
    {
        if (!CanResendOtp)
        {
            await _dialogService.ShowInfoAsync("????", "????? ??? ???? ??? ????");
            return;
        }

        await SendOtpAsync(cancellationToken);
    }

    #region Private Helper Methods

    private bool ValidateNationalCodeAndBirthDate()
    {
        if (string.IsNullOrWhiteSpace(NationalCode) || NationalCode.Length != 10)
        {
            ErrorMessage = "?? ??? ???? ?? ??? ????";
            HasError = true;
            return false;
        }

        if (string.IsNullOrWhiteSpace(BirthDate))
        {
            ErrorMessage = "????? ????? ???? ?? ???? ????";
            HasError = true;
            return false;
        }

        // Validate Persian date format (YYYY/MM/DD)
        if (!IsValidPersianDate(BirthDate))
        {
            ErrorMessage = "???? ????? ???? ???? ????. ???? ????: 1370/01/15";
            HasError = true;
            return false;
        }

        return true;
    }

    private bool IsValidPersianDate(string persianDate)
    {
        if (string.IsNullOrWhiteSpace(persianDate))
            return false;

        var parts = persianDate.Split('/');
        if (parts.Length != 3)
            return false;

        if (!int.TryParse(parts[0], out int year) ||
            !int.TryParse(parts[1], out int month) ||
            !int.TryParse(parts[2], out int day))
            return false;

        // Persian calendar validation
        if (year < 1300 || year > 1450)
            return false;

        if (month < 1 || month > 12)
            return false;

        if (day < 1 || day > 31)
            return false;

        return true;
    }

    private bool ValidatePhoneNumber()
    {
        if (string.IsNullOrWhiteSpace(Phone))
        {
            ErrorMessage = "????? ????? ?????? ?? ???? ????";
            HasError = true;
            return false;
        }

        Phone = Phone.Trim();

        if (!Phone.StartsWith("09") || Phone.Length != 11)
        {
            ErrorMessage = "????? ?????? ???? ?? ?? ???? ??? ? ?? ??? ????";
            HasError = true;
            return false;
        }

        if (!Phone.All(char.IsDigit))
        {
            ErrorMessage = "????? ?????? ??? ???? ???? ????? ????";
            HasError = true;
            return false;
        }

        return true;
    }

    private bool ValidateFinalData()
    {
        if (!IsVerified)
        {
            ErrorMessage = "????? ????? ???? ??? ?? ????? ????";
            HasError = true;
            return false;
        }

        if (!OtpSent || string.IsNullOrWhiteSpace(OtpCode))
        {
            ErrorMessage = "????? ?? ????? ?? ???? ????";
            HasError = true;
            return false;
        }

        if (string.IsNullOrWhiteSpace(Phone))
        {
            ErrorMessage = "????? ?????? ????? ???";
            HasError = true;
            return false;
        }

        return true;
    }

    private async Task VerifyPhoneWithShahkarAsync(CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Verifying phone with Shahkar: {Phone}, {NationalCode}", Phone, NationalCode);

            var shahkarResponse = await _zibalService.ShahkarInquiryAsync(new Models.Services.Identity.Zibal.ShahkarInquiry.Request
            {
                mobile = Phone,
                nationalCode = NationalCode
            });

            _logger.LogInformation("Shahkar response: Result={Result}, Matched={Matched}",
                shahkarResponse?.result, shahkarResponse?.data?.matched);

            if (shahkarResponse?.result == 100 && shahkarResponse.data?.matched == true)
            {
                PhoneVerified = true;
                // Store numeric status code as string for HubSpot compatibility
                // 100 = Verified and matched
                ShahkarStatus = "100";
                _logger.LogInformation("Shahkar verification successful - Status: 100 (Verified)");
            }
            else if (shahkarResponse?.result == 100 && shahkarResponse.data?.matched == false)
            {
                PhoneVerified = false;
                // 101 = Not matched (number belongs to different national code)
                ShahkarStatus = "101";
                _logger.LogWarning("Shahkar verification failed: Phone not matched with national code - Status: 101");
            }
            else
            {
                PhoneVerified = false;
                // Store actual result code from Zibal, or use 999 for unknown error
                ShahkarStatus = shahkarResponse?.result?.ToString() ?? "999";
                _logger.LogWarning("Shahkar verification returned unexpected result: {Result} - Status: {Status}",
                    shahkarResponse?.result, ShahkarStatus);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during Shahkar verification");
            PhoneVerified = false;
            // 500 = Error during verification
            ShahkarStatus = "500";
        }
    }

    private async Task SendWelcomeSmsAsync(CancellationToken cancellationToken)
    {
        try
        {
            await _smsService.SendWelcomeAsync(Phone, FirstName, LastName);
            _logger.LogInformation("Welcome SMS sent to: {Phone}", Phone);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to send welcome SMS");
        }
    }

    private void StartOtpTimer()
    {
        _otpTimer?.Dispose();
        _otpTimer = new System.Timers.Timer(1000);
        _otpTimer.Elapsed += (sender, e) => UpdateOtpTimer();
        _otpTimer.Start();
    }

    private void UpdateOtpTimer()
    {
        var remaining = _otpService.GetRemainingTime(Phone);
        if (remaining.HasValue && remaining.Value.TotalSeconds > 0)
        {
            OtpRemainingTime = $"{remaining.Value.Minutes:D2}:{remaining.Value.Seconds:D2}";
        }
        else
        {
            OtpRemainingTime = string.Empty;
            CanResendOtp = true;
            _otpTimer?.Stop();
        }
    }

    #endregion

    protected override void OnError(Exception exception)
    {
        _logger.LogError(exception, "Error in RegisterViewModel");
    }

    public void Dispose()
    {
        _otpTimer?.Dispose();
    }
}

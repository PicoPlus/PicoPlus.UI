#nullable enable

using PicoPlus.Application.Common.Interfaces;
using PicoPlus.Domain.Entities;
using PicoPlus.Domain.Events.Contact;
using PicoPlus.Infrastructure.Services;
using PicoPlus.Infrastructure.State;
using PicoPlus.Services.SMS;
using Microsoft.Extensions.Logging;

namespace PicoPlus.Services.Auth;

/// <summary>
/// Multi-step user registration flow as a plain Application-layer service.
/// </summary>
public class RegisterService : IRegisterService
{
    private readonly IContactRepository           _contactRepo;
    private readonly IIdentityVerificationService _identityService;
    private readonly IImageProcessingService      _imageService;
    private readonly ISmsService                  _smsService;
    private readonly OtpService                   _otpService;
    private readonly INavigationService           _navigationService;
    private readonly IDialogService               _dialogService;
    private readonly ISessionStorageService       _sessionStorage;
    private readonly AuthenticationStateService   _authState;
    private readonly ILogger<RegisterService>     _logger;

    // ── Step state ──────────────────────────────────────────────────────────
    public string NationalCode { get; set; } = string.Empty;
    public string BirthDate    { get; set; } = string.Empty;
    public string Phone        { get; set; } = string.Empty;
    public string OtpCode      { get; set; } = string.Empty;

    public string FirstName   { get; private set; } = string.Empty;
    public string LastName    { get; private set; } = string.Empty;
    public string FatherName  { get; private set; } = string.Empty;
    public string Gender      { get; private set; } = string.Empty;
    public bool   IsVerified  { get; private set; }
    public bool   OtpSent     { get; private set; }
    public bool   CanResendOtp { get; private set; } = true;
    public string OtpRemainingTime { get; private set; } = string.Empty;
    public int    CurrentStep { get; private set; } = 1;

    public bool   IsLoading    { get; private set; }
    public bool   HasError     { get; private set; }
    public string ErrorMessage { get; private set; } = string.Empty;

    public  string? ShahkarStatus   { get; private set; }
    private System.Timers.Timer? _otpTimer;

    private readonly IDomainEventDispatcher _eventDispatcher;

    public RegisterService(
        IContactRepository contactRepo,
        IIdentityVerificationService identityService,
        IImageProcessingService imageService,
        ISmsService smsService,
        OtpService otpService,
        INavigationService navigationService,
        IDialogService dialogService,
        ISessionStorageService sessionStorage,
        AuthenticationStateService authState,
        IDomainEventDispatcher eventDispatcher,
        ILogger<RegisterService> logger)
    {
        _contactRepo       = contactRepo;
        _identityService   = identityService;
        _imageService      = imageService;
        _smsService        = smsService;
        _otpService        = otpService;
        _navigationService = navigationService;
        _dialogService     = dialogService;
        _sessionStorage    = sessionStorage;
        _authState         = authState;
        _eventDispatcher   = eventDispatcher;
        _logger            = logger;
    }

    public async Task InitializeAsync(CancellationToken ct = default)
    {
        var pending = await _sessionStorage.GetItemAsync<string>("PendingNationalCode", ct);
        if (!string.IsNullOrEmpty(pending))
        {
            NationalCode = pending;
            await _sessionStorage.RemoveItemAsync("PendingNationalCode", ct);
        }
    }

    public async Task VerifyNationalIdentityAsync(CancellationToken ct = default)
    {
        if (!ValidateNationalCodeAndBirthDate()) return;
        await ExecuteAsync(async () =>
        {
            var result = await _identityService.VerifyNationalIdentityAsync(NationalCode, BirthDate);
            if (result.IsValid)
            {
                FirstName   = result.FirstName  ?? string.Empty;
                LastName    = result.LastName   ?? string.Empty;
                FatherName  = result.FatherName ?? string.Empty;
                IsVerified  = true;
                CurrentStep = 2;
                HasError    = false;
                ErrorMessage = string.Empty;
                await _dialogService.ShowSuccessAsync("هویت تأیید شد", $"اطلاعات شما با موفقیت تأیید شد: {FirstName} {LastName}");
            }
            else
            {
                await _dialogService.ShowErrorAsync("خطا در تأیید هویت", result.ErrorMessage ?? "تأیید هویت ناموفق بود.");
                IsVerified = false;
                FirstName = LastName = FatherName = Gender = string.Empty;
            }
        }, ct);
    }

    public async Task SendOtpAsync(CancellationToken ct = default)
    {
        if (!ValidatePhoneNumber()) return;
        await ExecuteAsync(async () =>
        {
            var otp = _otpService.GenerateOtp();
            _otpService.StoreOtp(Phone, otp);
            await _smsService.SendOtpAsync(Phone, otp);
            OtpSent      = true;
            CurrentStep  = 3;
            CanResendOtp = false;
            StartOtpTimer();
        }, ct);
    }

    public async Task VerifyOtpAsync(CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(OtpCode)) { SetError("کد تأیید را وارد کنید"); return; }
        await ExecuteAsync(async () =>
        {
            var result = _otpService.ValidateOtp(Phone, OtpCode);
            if (result.IsValid)
            {
                await _dialogService.ShowSuccessAsync("تأیید موفق", "شماره موبایل با موفقیت تأیید شد!");
                ShahkarStatus = "0"; // Shahkar verification disabled
                CurrentStep  = 4;
                HasError     = false;
                ErrorMessage = string.Empty;
            }
            else
            {
                SetError(result.ErrorMessage);
                await _dialogService.ShowErrorAsync("کد نادرست", result.ErrorMessage);
            }
        }, ct);
    }

    public async Task RegisterAsync(CancellationToken ct = default)
    {
        if (!ValidateFinalData()) return;
        await ExecuteAsync(async () =>
        {
            var newContact = new Contact
            {
                Id           = string.Empty,
                FirstName    = FirstName,
                LastName     = LastName,
                NationalCode = NationalCode,
                Phone        = Phone,
                Email        = $"{NationalCode}@picoplus.app",
                DateOfBirth  = BirthDate,
                FatherName   = FatherName,
                Gender       = Gender,
                ShahkarStatus = ShahkarStatus ?? "0"
            };

            var created = await _contactRepo.CreateAsync(newContact);
            if (!string.IsNullOrEmpty(created.Id))
            {
                _authState.SetAuthenticatedUser(created);
                var dto = _authState.CurrentUser;
                await _sessionStorage.SetItemAsync("LogInState",   1,   ct);
                await _sessionStorage.SetItemAsync("ContactModel", dto, ct);

                try { await _smsService.SendWelcomeAsync(Phone, FirstName, LastName); }
                catch (Exception ex) { _logger.LogWarning(ex, "Welcome SMS failed"); }

                // Raise domain event — handled asynchronously by ContactRegisteredHandler.
                await _eventDispatcher.DispatchAsync(new ContactRegisteredEvent
                {
                    ContactId    = created.Id,
                    FirstName    = FirstName,
                    LastName     = LastName,
                    Phone        = Phone,
                    Email        = created.Email,
                    NationalCode = NationalCode,
                    ShahkarStatus = ShahkarStatus,
                }, ct);

                await _dialogService.ShowSuccessAsync("ثبت نام موفق", $"خوش آمدید {FirstName} {LastName}!");
                _navigationService.NavigateTo("/user");
            }
            else
            {
                await _dialogService.ShowErrorAsync("خطا", "خطا در ایجاد حساب کاربری");
            }
        }, ct);
    }

    public async Task ResendOtpAsync(CancellationToken ct = default)
    {
        if (!CanResendOtp) { await _dialogService.ShowInfoAsync("صبر کنید", "درخواست بعدی هنوز آماده نشده"); return; }
        await SendOtpAsync(ct);
    }

    // ── Private helpers ──────────────────────────────────────────────────────

    private async Task VerifyPhoneWithShahkarAsync(CancellationToken ct)
    {
        try
        {
            var result = await _identityService.VerifyPhoneOwnershipAsync(NationalCode, Phone);
            ShahkarStatus = result.IsMatched ? "100" : "101";
            if (!result.IsMatched)
                _logger.LogWarning("Shahkar: phone {Phone} not matched for national code {NCode} — flagged as 101, registration continues",
                    Phone, NationalCode);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Shahkar check failed — flagged as 500, registration continues");
            ShahkarStatus = "500";
        }
    }

    private async Task ExecuteAsync(Func<Task> operation, CancellationToken ct)
    {
        IsLoading    = true;
        HasError     = false;
        ErrorMessage = string.Empty;
        try   { await operation(); }
        catch (Exception ex)
        {
            _logger.LogError(ex, "RegisterService error");
            SetError(ex.Message);
        }
        finally { IsLoading = false; }
    }

    private void SetError(string? msg) { HasError = true; ErrorMessage = msg ?? string.Empty; }

    private bool ValidateNationalCodeAndBirthDate()
    {
        if (string.IsNullOrWhiteSpace(NationalCode) || NationalCode.Length != 10)
            return SetErrorReturn("کد ملی باید ده رقم باشد");
        if (string.IsNullOrWhiteSpace(BirthDate))
            return SetErrorReturn("تاریخ تولد باید وارد شود");
        var parts = BirthDate.Split('/');
        if (parts.Length != 3 || !int.TryParse(parts[0], out int y) || y < 1300)
            return SetErrorReturn("فرمت تاریخ صحیح نیست. مثال: 1370/01/15");
        return true;
    }

    private bool ValidatePhoneNumber()
    {
        Phone = Phone.Trim();
        if (!Phone.StartsWith("09") || Phone.Length != 11 || !Phone.All(char.IsDigit))
            return SetErrorReturn("شماره موبایل باید با ۰۹ شروع شده و ۱۱ رقم باشد");
        return true;
    }

    private bool ValidateFinalData()
    {
        if (!IsVerified)      return SetErrorReturn("تأیید هویت ملی را ابتدا انجام دهید");
        if (!OtpSent || string.IsNullOrWhiteSpace(OtpCode)) return SetErrorReturn("کد تأیید را وارد کنید");
        if (string.IsNullOrWhiteSpace(Phone)) return SetErrorReturn("شماره موبایل خالی است");
        return true;
    }

    private bool SetErrorReturn(string msg) { SetError(msg); return false; }

    private void StartOtpTimer()
    {
        _otpTimer?.Dispose();
        _otpTimer = new System.Timers.Timer(1000);
        _otpTimer.Elapsed += (_, _) =>
        {
            var remaining = _otpService.GetRemainingTime(Phone);
            if (remaining.HasValue && remaining.Value.TotalSeconds > 0)
                OtpRemainingTime = $"{remaining.Value.Minutes:D2}:{remaining.Value.Seconds:D2}";
            else { OtpRemainingTime = string.Empty; CanResendOtp = true; _otpTimer?.Stop(); }
        };
        _otpTimer.Start();
    }

    public void Dispose() => _otpTimer?.Dispose();
}

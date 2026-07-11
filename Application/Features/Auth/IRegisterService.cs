#nullable enable

namespace PicoPlus.Services.Auth;

/// <summary>
/// Multi-step user registration flow.
/// Clean Architecture: depends only on Application interfaces.
/// </summary>
public interface IRegisterService
{
    // ── Step state ──────────────────────────────────────────────────────────
    string NationalCode { get; set; }
    string BirthDate    { get; set; }
    string Phone        { get; set; }
    string OtpCode      { get; set; }

    string FirstName      { get; }
    string LastName       { get; }
    string FatherName     { get; }
    string Gender         { get; }
    bool   IsVerified     { get; }
    bool   OtpSent        { get; }
    /// <summary>null = not checked yet, "100" = matched, "101" = not matched, "500" = error</summary>
    string? ShahkarStatus { get; }
    bool   CanResendOtp { get; }
    string OtpRemainingTime { get; }

    int    CurrentStep { get; }

    bool   IsLoading     { get; }
    bool   HasError      { get; }
    string ErrorMessage  { get; }

    // ── Commands ────────────────────────────────────────────────────────────
    Task InitializeAsync(CancellationToken ct = default);
    Task VerifyNationalIdentityAsync(CancellationToken ct = default);
    Task SendOtpAsync(CancellationToken ct = default);
    Task VerifyOtpAsync(CancellationToken ct = default);
    Task RegisterAsync(CancellationToken ct = default);
    Task ResendOtpAsync(CancellationToken ct = default);
    void Dispose();
}

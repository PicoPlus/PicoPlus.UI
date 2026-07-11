#nullable enable

namespace PicoPlus.Application.Common.Interfaces;

/// <summary>
/// Application-layer contract for Iranian national identity verification (Zibal).
/// </summary>
public interface IIdentityVerificationService
{
    Task<IdentityVerificationResult> VerifyNationalIdentityAsync(string nationalCode, string birthDate);
    Task<ShahkarVerificationResult> VerifyPhoneOwnershipAsync(string nationalCode, string phoneNumber);
}

public sealed record IdentityVerificationResult(
    bool IsValid,
    string? FirstName,
    string? LastName,
    string? FatherName,
    string? BirthDate,
    string? ErrorMessage = null);

public sealed record ShahkarVerificationResult(
    bool IsMatched,
    string? ErrorMessage = null);

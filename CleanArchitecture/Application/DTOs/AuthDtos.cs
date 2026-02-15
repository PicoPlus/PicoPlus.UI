namespace PicoPlus.CleanArchitecture.Application.DTOs;

public sealed record LoginByNationalCodeRequest(string NationalCode, string SelectedRole);

public sealed record LoginByNationalCodeResult(
    bool IsSuccess,
    string? ErrorMessage,
    string? UserId,
    string RedirectUri,
    bool RequiresRegistration = false);

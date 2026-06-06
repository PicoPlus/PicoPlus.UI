namespace PicoPlus.Services.Shared;

/// <summary>
/// Shared validator for Iranian national codes (کد ملی).
/// Consolidates validation logic duplicated across LoginViewModel and RegisterViewModel.
/// </summary>
public static class NationalCodeValidator
{
    /// <summary>
    /// Full validation: checks empty, length, digits-only, and checksum.
    /// Returns null if valid, or an error message string if invalid.
    /// </summary>
    public static string? Validate(string? nationalCode)
    {
        if (string.IsNullOrWhiteSpace(nationalCode))
            return "کد ملی نمی‌تواند خالی باشد";

        nationalCode = nationalCode.Trim();

        if (nationalCode.Length != 10)
            return "کد ملی باید ده رقم باشد";

        if (!nationalCode.All(char.IsDigit))
            return "کد ملی می‌تواند فقط شامل اعداد باشد";

        if (!IsValidChecksum(nationalCode))
            return "کد ملی معتبر نیست";

        return null;
    }

    /// <summary>
    /// Validates Iranian national code using the checksum algorithm.
    /// </summary>
    public static bool IsValidChecksum(string nationalCode)
    {
        if (nationalCode.Length != 10)
            return false;

        if (nationalCode.All(c => c == nationalCode[0]))
            return false;

        var sum = 0;
        for (var i = 0; i < 9; i++)
        {
            sum += int.Parse(nationalCode[i].ToString()) * (10 - i);
        }

        var remainder = sum % 11;
        var checkDigit = remainder < 2 ? remainder : 11 - remainder;

        return checkDigit == int.Parse(nationalCode[9].ToString());
    }
}

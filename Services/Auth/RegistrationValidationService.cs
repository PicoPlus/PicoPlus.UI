namespace PicoPlus.Services.Auth;

public interface IRegistrationValidationService
{
    bool ValidateNationalCodeAndBirthDate(string nationalCode, string birthDate, out string errorMessage);
    bool ValidatePhoneNumber(string phone, out string errorMessage);
    bool ValidateFinalData(bool isVerified, bool otpSent, string otpCode, string phone, out string errorMessage);
}

public class RegistrationValidationService : IRegistrationValidationService
{
    public bool ValidateNationalCodeAndBirthDate(string nationalCode, string birthDate, out string errorMessage)
    {
        if (string.IsNullOrWhiteSpace(nationalCode) || nationalCode.Length != 10)
        {
            errorMessage = "کد ملی باید ده رقم باشد";
            return false;
        }

        if (string.IsNullOrWhiteSpace(birthDate))
        {
            errorMessage = "تاریخ تولد را وارد کنید";
            return false;
        }

        if (!IsValidPersianDate(birthDate))
        {
            errorMessage = "فرمت تاریخ صحیح نیست. مثال صحیح: 1370/01/15";
            return false;
        }

        errorMessage = string.Empty;
        return true;
    }

    public bool ValidatePhoneNumber(string phone, out string errorMessage)
    {
        if (string.IsNullOrWhiteSpace(phone))
        {
            errorMessage = "شماره موبایل را وارد کنید";
            return false;
        }

        var normalized = phone.Trim();

        if (!normalized.StartsWith("09") || normalized.Length != 11)
        {
            errorMessage = "شماره موبایل باید با 09 شروع شده و 11 رقم باشد";
            return false;
        }

        if (!normalized.All(char.IsDigit))
        {
            errorMessage = "شماره موبایل فقط باید شامل ارقام باشد";
            return false;
        }

        errorMessage = string.Empty;
        return true;
    }

    public bool ValidateFinalData(bool isVerified, bool otpSent, string otpCode, string phone, out string errorMessage)
    {
        if (!isVerified)
        {
            errorMessage = "ابتدا هویت خود را تایید کنید";
            return false;
        }

        if (!otpSent || string.IsNullOrWhiteSpace(otpCode))
        {
            errorMessage = "کد تایید را وارد کنید";
            return false;
        }

        if (string.IsNullOrWhiteSpace(phone))
        {
            errorMessage = "شماره موبایل معتبر نیست";
            return false;
        }

        errorMessage = string.Empty;
        return true;
    }

    private static bool IsValidPersianDate(string persianDate)
    {
        if (string.IsNullOrWhiteSpace(persianDate)) return false;

        var parts = persianDate.Split('/');
        if (parts.Length != 3) return false;

        if (!int.TryParse(parts[0], out var year) ||
            !int.TryParse(parts[1], out var month) ||
            !int.TryParse(parts[2], out var day))
        {
            return false;
        }

        return year is >= 1300 and <= 1450 && month is >= 1 and <= 12 && day is >= 1 and <= 31;
    }
}

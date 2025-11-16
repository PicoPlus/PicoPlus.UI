using System.Globalization;

namespace PicoPlus.Services.Utils;

/// <summary>
/// Helper class for Persian (Jalali/Shamsi) calendar conversions
/// </summary>
public static class PersianCalendarHelper
{
    private static readonly PersianCalendar _persianCalendar = new();

    /// <summary>
    /// Convert Gregorian DateTime to Persian date string (yyyy/MM/dd)
    /// </summary>
    public static string ToPersianDate(DateTime gregorianDate)
    {
        var year = _persianCalendar.GetYear(gregorianDate);
        var month = _persianCalendar.GetMonth(gregorianDate);
        var day = _persianCalendar.GetDayOfMonth(gregorianDate);
        return $"{year}/{month:D2}/{day:D2}";
    }

    /// <summary>
    /// Convert Gregorian DateTime to Persian date string with custom format
    /// </summary>
    public static string ToPersianDate(DateTime gregorianDate, string format)
    {
        var year = _persianCalendar.GetYear(gregorianDate);
        var month = _persianCalendar.GetMonth(gregorianDate);
        var day = _persianCalendar.GetDayOfMonth(gregorianDate);

        return format
            .Replace("yyyy", year.ToString())
            .Replace("yy", (year % 100).ToString("D2"))
            .Replace("MM", month.ToString("D2"))
            .Replace("M", month.ToString())
            .Replace("dd", day.ToString("D2"))
            .Replace("d", day.ToString());
    }

    /// <summary>
    /// Convert Persian date string (yyyy/MM/dd) to Gregorian DateTime
    /// </summary>
    public static DateTime? ToGregorianDate(string persianDate)
    {
        try
        {
            var parts = persianDate.Split('/');
            if (parts.Length != 3)
                return null;

            if (!int.TryParse(parts[0], out var year) ||
                !int.TryParse(parts[1], out var month) ||
                !int.TryParse(parts[2], out var day))
                return null;

            return _persianCalendar.ToDateTime(year, month, day, 0, 0, 0, 0);
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Get Persian month name
    /// </summary>
    public static string GetPersianMonthName(int month)
    {
        return month switch
        {
            1 => "???????",
            2 => "????????",
            3 => "?????",
            4 => "???",
            5 => "?????",
            6 => "??????",
            7 => "???",
            8 => "????",
            9 => "???",
            10 => "??",
            11 => "????",
            12 => "?????",
            _ => throw new ArgumentOutOfRangeException(nameof(month))
        };
    }

    /// <summary>
    /// Get Persian day of week name
    /// </summary>
    public static string GetPersianDayOfWeek(DayOfWeek dayOfWeek)
    {
        return dayOfWeek switch
        {
            DayOfWeek.Saturday => "????",
            DayOfWeek.Sunday => "??????",
            DayOfWeek.Monday => "??????",
            DayOfWeek.Tuesday => "???????",
            DayOfWeek.Wednesday => "????????",
            DayOfWeek.Thursday => "????????",
            DayOfWeek.Friday => "????",
            _ => throw new ArgumentOutOfRangeException(nameof(dayOfWeek))
        };
    }

    /// <summary>
    /// Get full Persian date with day name
    /// Example: "????? ?? ??????? ????"
    /// </summary>
    public static string ToFullPersianDate(DateTime gregorianDate)
    {
        var year = _persianCalendar.GetYear(gregorianDate);
        var month = _persianCalendar.GetMonth(gregorianDate);
        var day = _persianCalendar.GetDayOfMonth(gregorianDate);
        var dayOfWeek = GetPersianDayOfWeek(gregorianDate.DayOfWeek);
        var monthName = GetPersianMonthName(month);

        return $"{dayOfWeek}? {day} {monthName} {year}";
    }

    /// <summary>
    /// Convert numbers to Persian digits
    /// </summary>
    public static string ToPersianDigits(string input)
    {
        if (string.IsNullOrEmpty(input))
            return input;

        var result = input;
        result = result.Replace('0', '?');
        result = result.Replace('1', '?');
        result = result.Replace('2', '?');
        result = result.Replace('3', '?');
        result = result.Replace('4', '?');
        result = result.Replace('5', '?');
        result = result.Replace('6', '?');
        result = result.Replace('7', '?');
        result = result.Replace('8', '?');
        result = result.Replace('9', '?');
        return result;
    }

    /// <summary>
    /// Validate Persian date string
    /// </summary>
    public static bool IsValidPersianDate(string persianDate)
    {
        try
        {
            var parts = persianDate.Split('/');
            if (parts.Length != 3)
                return false;

            if (!int.TryParse(parts[0], out var year) ||
                !int.TryParse(parts[1], out var month) ||
                !int.TryParse(parts[2], out var day))
                return false;

            if (year < 1300 || year > 1500)
                return false;

            if (month < 1 || month > 12)
                return false;

            // Check day based on month
            var maxDays = month <= 6 ? 31 : (month <= 11 ? 30 : 29);

            // Leap year check for month 12
            if (month == 12 && IsLeapYear(year))
                maxDays = 30;

            if (day < 1 || day > maxDays)
                return false;

            return true;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Check if a Persian year is leap year
    /// </summary>
    public static bool IsLeapYear(int persianYear)
    {
        return _persianCalendar.IsLeapYear(persianYear);
    }

    /// <summary>
    /// Get age from Persian birth date
    /// </summary>
    public static int GetAge(string persianBirthDate)
    {
        var birthDate = ToGregorianDate(persianBirthDate);
        if (!birthDate.HasValue)
            return 0;

        var today = DateTime.Today;
        var age = today.Year - birthDate.Value.Year;

        if (birthDate.Value.Date > today.AddYears(-age))
            age--;

        return age;
    }

    /// <summary>
    /// Get current Persian date
    /// </summary>
    public static string GetCurrentPersianDate()
    {
        return ToPersianDate(DateTime.Now);
    }

    /// <summary>
    /// Format Persian date for HubSpot (yyyy/MM/dd format)
    /// </summary>
    public static string FormatForHubSpot(DateTime gregorianDate)
    {
        return ToPersianDate(gregorianDate);
    }
}

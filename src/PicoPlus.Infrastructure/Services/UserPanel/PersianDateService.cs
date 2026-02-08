#nullable enable

using System.Globalization;

namespace PicoPlus.Application.Abstractions.UserPanel;

/// <summary>
/// Implementation of Persian date service
/// Thread-safe singleton service for date formatting
/// </summary>
public class PersianDateService : IPersianDateService
{
    private readonly PersianCalendar _persianCalendar = new();

    public string FormatDate(DateTime date)
    {
        try
        {
            int year = _persianCalendar.GetYear(date);
            int month = _persianCalendar.GetMonth(date);
            int day = _persianCalendar.GetDayOfMonth(date);
            return $"{year:D4}/{month:D2}/{day:D2}";
        }
        catch
        {
            return date.ToString("yyyy/MM/dd");
        }
    }

    public string FormatDate(DateTime? date)
    {
        if (date == null) return "-";
        return FormatDate(date.Value);
    }

    public string FormatDate(string? dateString)
    {
        if (string.IsNullOrEmpty(dateString)) return "-";

        if (DateTime.TryParse(dateString, out var date))
        {
            return FormatDate(date);
        }

        return dateString;
    }

    public string FormatNumber(decimal? number)
    {
        if (number == null) return "0";
        return string.Format("{0:N0}", number).Replace(",", "?");
    }
}

#nullable enable

using PicoPlus.Application.Dto.UserPanel;

namespace PicoPlus.Application.Abstractions.UserPanel;

/// <summary>
/// Service for Persian date formatting and conversion
/// Registered as Singleton for performance
/// </summary>
public interface IPersianDateService
{
    /// <summary>
    /// Format DateTime to Persian date string (yyyy/MM/dd)
    /// </summary>
    string FormatDate(DateTime date);

    /// <summary>
    /// Format nullable DateTime to Persian date string
    /// </summary>
    string FormatDate(DateTime? date);

    /// <summary>
    /// Format string date to Persian date
    /// </summary>
    string FormatDate(string? dateString);

    /// <summary>
    /// Format decimal number with Persian separators
    /// </summary>
    string FormatNumber(decimal? number);
}

namespace NovinCRM.Localization.Models;

/// <summary>
/// Describes a missing-key event, used for diagnostics and optional reporting pipelines.
/// </summary>
/// <param name="Key">The key that was requested but not found.</param>
/// <param name="LanguageCode">The active language at the time of the miss.</param>
/// <param name="FallbackAttempted">Whether a fallback language lookup was attempted.</param>
public sealed record MissingKeyRecord(
    string Key,
    string LanguageCode,
    bool FallbackAttempted);

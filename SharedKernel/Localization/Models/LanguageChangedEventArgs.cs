namespace NovinCRM.Localization.Models;

/// <summary>
/// Event arguments raised by <see cref="NovinCRM.Localization.Abstractions.ILocalizationService.LanguageChanged"/>.
/// </summary>
public sealed class LanguageChangedEventArgs : EventArgs
{
    /// <summary>Language code that was active before the switch.</summary>
    public string PreviousLanguage { get; }

    /// <summary>Language code that is now active.</summary>
    public string NewLanguage { get; }

    /// <summary>
    /// Initializes a new instance of <see cref="LanguageChangedEventArgs"/>.
    /// </summary>
    public LanguageChangedEventArgs(string previousLanguage, string newLanguage)
    {
        PreviousLanguage = previousLanguage;
        NewLanguage = newLanguage;
    }
}

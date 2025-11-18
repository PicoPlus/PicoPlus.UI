namespace PicoPlus.ViewModels.Auth;

/// <summary>
/// Base interface for authentication ViewModels
/// Provides common properties for Login and AdminLogin ViewModels
/// </summary>
public interface IAuthBaseViewModel
{
    /// <summary>
    /// Indicates if the ViewModel is currently performing an operation
    /// </summary>
    bool IsLoading { get; set; }

    /// <summary>
    /// Indicates if there is an error
    /// </summary>
    bool HasError { get; set; }

    /// <summary>
    /// Error message to display to the user
    /// </summary>
    string ErrorMessage { get; set; }
}

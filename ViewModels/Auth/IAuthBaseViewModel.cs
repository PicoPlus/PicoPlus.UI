namespace PicoPlus.ViewModels.Auth;

/// <summary>
/// Common interface for authentication view models
/// </summary>
public interface IAuthBaseViewModel
{
    bool HasError { get; }
    string ErrorMessage { get; }
    bool IsLoading { get; }
}

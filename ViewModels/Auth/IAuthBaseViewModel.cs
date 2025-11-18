namespace PicoPlus.ViewModels.Auth;

/// <summary>
/// Base interface for authentication ViewModels
/// </summary>
public interface IAuthBaseViewModel
{
    bool IsLoading { get; }
    bool HasError { get; }
    string ErrorMessage { get; }
}

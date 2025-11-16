using CommunityToolkit.Mvvm.ComponentModel;

namespace PicoPlus.ViewModels;

/// <summary>
/// Base class for all ViewModels providing common functionality
/// </summary>
public abstract partial class BaseViewModel : ObservableObject
{
    [ObservableProperty]
    private bool _isBusy;

    [ObservableProperty]
    private string _title = string.Empty;

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private string _errorMessage = string.Empty;

    [ObservableProperty]
    private bool _hasError;

    /// <summary>
    /// Executes an async operation with automatic busy state management
    /// </summary>
    protected async Task ExecuteAsync(Func<Task> operation, CancellationToken cancellationToken = default)
    {
        if (IsBusy)
            return;

        IsBusy = true;
        IsLoading = true;
        HasError = false;
        ErrorMessage = string.Empty;

        try
        {
            await operation();
        }
        catch (Exception ex)
        {
            HasError = true;
            ErrorMessage = ex.Message;
            OnError(ex);
        }
        finally
        {
            IsBusy = false;
            IsLoading = false;
        }
    }

    /// <summary>
    /// Override to handle errors in derived classes
    /// </summary>
    protected virtual void OnError(Exception exception)
    {
        // Log error - can be overridden in derived classes
    }
}

using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Extensions.Logging;

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
    /// Executes an async operation with automatic busy state management.
    /// Exceptions are caught, surfaced via HasError/ErrorMessage, and logged.
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
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            // Intentional cancellation — don't surface as an error
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
    /// Override to handle errors in derived classes.
    /// Default implementation logs a warning using the runtime logger for the concrete type.
    /// </summary>
    protected virtual void OnError(Exception exception)
    {
        // Resolve a logger for the concrete ViewModel type at call time so that
        // derived classes that don't override OnError still get meaningful output.
        var loggerFactory = GetLoggerFactory();
        loggerFactory?.CreateLogger(GetType())?.LogWarning(exception,
            "Unhandled error in {ViewModelType}", GetType().Name);
    }

    /// <summary>
    /// Override to provide a logger factory for base-class error logging.
    /// Returns null by default; derived classes that inject ILogger can override this.
    /// </summary>
    protected virtual ILoggerFactory? GetLoggerFactory() => null;
}

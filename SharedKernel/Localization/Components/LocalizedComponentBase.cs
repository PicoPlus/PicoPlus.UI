using Microsoft.AspNetCore.Components;
using NovinCRM.Localization.Abstractions;
using NovinCRM.Localization.Services;

namespace NovinCRM.Localization.Components;

/// <summary>
/// Base class for all Blazor components that consume localization.
///
/// Inherit from this instead of <see cref="ComponentBase"/> to get:
/// <list type="bullet">
///   <item>Automatic language initialisation from LocalStorage on first render.</item>
///   <item>Automatic re-render when the user switches language at runtime.</item>
///   <item>Convenient <c>L</c> property pre-injected and ready to use.</item>
///   <item>RTL/LTR CSS helpers.</item>
/// </list>
///
/// Usage:
/// <code>
/// @inherits LocalizedComponentBase
///
/// &lt;h1&gt;@L["DashboardTitle"]&lt;/h1&gt;
/// &lt;div dir="@L.TextDirection"&gt; ... &lt;/div&gt;
/// </code>
/// </summary>
public abstract class LocalizedComponentBase : ComponentBase, IAsyncDisposable
{
    /// <summary>
    /// The localization service injected for this component.
    /// Use <c>L["Key"]</c> or <c>L["Key", arg1, arg2]</c> in Razor markup.
    /// </summary>
    [Inject]
    protected ILocalizationService L { get; set; } = default!;

    [Inject]
    private ScopedLocalizationService ScopedL { get; set; } = default!;

    // ── Lifecycle ──────────────────────────────────────────────────────────────

    /// <inheritdoc/>
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            // LocalStorage is only accessible after the first render (JS interop).
            await ScopedL.InitialiseAsync();

            // Subscribe to language changes so the component re-renders on switch.
            L.LanguageChanged += OnLanguageChanged;

            // Trigger a re-render now that language is loaded from LocalStorage.
            await InvokeAsync(StateHasChanged);
        }

        await base.OnAfterRenderAsync(firstRender);
    }

    // ── Language change handler ────────────────────────────────────────────────

    private void OnLanguageChanged(object? sender, Localization.Models.LanguageChangedEventArgs e)
    {
        // Marshal to Blazor's synchronization context and re-render.
        InvokeAsync(StateHasChanged);
    }

    // ── Helpers ────────────────────────────────────────────────────────────────

    /// <summary>Returns "rtl" or "ltr" for use in HTML <c>dir</c> attributes.</summary>
    protected string Dir => L.TextDirection;

    /// <summary>
    /// Returns Bootstrap RTL class suffix when the current language is RTL.
    /// Example: use <c>@RtlSuffix</c> to append "-rtl" to Bootstrap class names.
    /// </summary>
    protected string RtlClass => L.IsRtl ? "rtl" : string.Empty;

    // ── IAsyncDisposable ───────────────────────────────────────────────────────

    /// <inheritdoc/>
    public virtual ValueTask DisposeAsync()
    {
        L.LanguageChanged -= OnLanguageChanged;
        return ValueTask.CompletedTask;
    }
}

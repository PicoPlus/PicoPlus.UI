#nullable enable

using Microsoft.AspNetCore.Components.Authorization;
using System.Security.Claims;

namespace PicoPlus.Extensions;

/// <summary>
/// Extension methods for security and authentication
/// </summary>
public static class SecurityExtensions
{
    /// <summary>
    /// Get user ID from ClaimsPrincipal
    /// </summary>
    public static string? GetUserId(this ClaimsPrincipal user)
    {
        return user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
    }

    /// <summary>
    /// Get user email from ClaimsPrincipal
    /// </summary>
    public static string? GetUserEmail(this ClaimsPrincipal user)
    {
        return user.FindFirst(ClaimTypes.Email)?.Value;
    }

    /// <summary>
    /// Get user name from ClaimsPrincipal
    /// </summary>
    public static string? GetUserName(this ClaimsPrincipal user)
    {
        return user.FindFirst(ClaimTypes.Name)?.Value;
    }

    /// <summary>
    /// Check if user is in specified role
    /// </summary>
    public static bool IsInRole(this ClaimsPrincipal user, string role)
    {
        return user.IsInRole(role);
    }

    /// <summary>
    /// Get authenticated user from AuthenticationState
    /// </summary>
    public static async Task<ClaimsPrincipal?> GetAuthenticatedUserAsync(this AuthenticationStateProvider provider)
    {
        var state = await provider.GetAuthenticationStateAsync();
        return state.User.Identity?.IsAuthenticated == true ? state.User : null;
    }

    /// <summary>
    /// HTML encode string for XSS prevention
    /// </summary>
    public static string HtmlEncode(this string? value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return string.Empty;
        }

        return System.Web.HttpUtility.HtmlEncode(value);
    }

    /// <summary>
    /// Sanitize string by removing potentially dangerous characters.
    /// Uses iterative stripping and regex to prevent bypass via nested/obfuscated payloads.
    /// For rendering user content in HTML, prefer HtmlEncode over Sanitize.
    /// </summary>
    public static string Sanitize(this string? value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return string.Empty;
        }

        var sanitized = value;

        // Iteratively strip script tags (handles nested variants like <scr<script>ipt>)
        string previous;
        do
        {
            previous = sanitized;
            sanitized = System.Text.RegularExpressions.Regex.Replace(
                sanitized, @"<\s*/?\s*script[^>]*>", "", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        } while (sanitized != previous);

        // Strip event handler attributes (onclick, onerror, onload, onmouseover, etc.)
        sanitized = System.Text.RegularExpressions.Regex.Replace(
            sanitized, @"\bon\w+\s*=", "", System.Text.RegularExpressions.RegexOptions.IgnoreCase);

        // Strip javascript: / vbscript: / data: URI schemes
        sanitized = System.Text.RegularExpressions.Regex.Replace(
            sanitized, @"(javascript|vbscript|data)\s*:", "", System.Text.RegularExpressions.RegexOptions.IgnoreCase);

        return sanitized;
    }
}

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
    /// Sanitize string by removing potentially dangerous characters
    /// </summary>
    public static string Sanitize(this string? value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return string.Empty;
        }

        // Remove script tags and potentially dangerous content
        var sanitized = value
            .Replace("<script>", "", StringComparison.OrdinalIgnoreCase)
            .Replace("</script>", "", StringComparison.OrdinalIgnoreCase)
            .Replace("javascript:", "", StringComparison.OrdinalIgnoreCase)
            .Replace("onerror=", "", StringComparison.OrdinalIgnoreCase)
            .Replace("onclick=", "", StringComparison.OrdinalIgnoreCase);

        return sanitized;
    }
}

using PicoPlus.Infrastructure.State;

namespace PicoPlus.Tests;

public class AuthenticationStateServiceTests
{
    [Fact]
    public void SetAdminAuthenticatedUser_SetsAdminFlags()
    {
        var service = new AuthenticationStateService();

        service.SetAdminAuthenticatedUser("admin@picoplus.app", "Admin");

        Assert.True(service.IsAuthenticated);
        Assert.True(service.IsAdminAuthenticated);
        Assert.Equal("admin@picoplus.app", service.AdminEmail);
        Assert.Equal("Admin", service.AdminDisplayName);
    }

    [Fact]
    public void ClearAuthentication_ClearsAdminState()
    {
        var service = new AuthenticationStateService();
        service.SetAdminAuthenticatedUser("admin@picoplus.app", "Admin");

        service.ClearAuthentication();

        Assert.False(service.IsAuthenticated);
        Assert.False(service.IsAdminAuthenticated);
        Assert.Equal(string.Empty, service.AdminEmail);
    }
}

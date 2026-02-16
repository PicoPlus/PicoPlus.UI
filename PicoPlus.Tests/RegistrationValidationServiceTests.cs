using PicoPlus.Services.Auth;

namespace PicoPlus.Tests;

public class RegistrationValidationServiceTests
{
    private readonly RegistrationValidationService _sut = new();

    [Fact]
    public void ValidateNationalCodeAndBirthDate_ReturnsFalse_ForInvalidDate()
    {
        var valid = _sut.ValidateNationalCodeAndBirthDate("1234567890", "invalid", out var error);

        Assert.False(valid);
        Assert.False(string.IsNullOrWhiteSpace(error));
    }

    [Fact]
    public void ValidatePhoneNumber_ReturnsFalse_ForNonIranianMobileFormat()
    {
        var valid = _sut.ValidatePhoneNumber("9123456789", out var error);

        Assert.False(valid);
        Assert.False(string.IsNullOrWhiteSpace(error));
    }

    [Fact]
    public void ValidateFinalData_ReturnsTrue_ForCompleteInputs()
    {
        var valid = _sut.ValidateFinalData(true, true, "123456", "09123456789", out var error);

        Assert.True(valid);
        Assert.Equal(string.Empty, error);
    }
}

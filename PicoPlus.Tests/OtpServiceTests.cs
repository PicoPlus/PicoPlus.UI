using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using PicoPlus.Services.Auth;

namespace PicoPlus.Tests;

public class OtpServiceTests
{
    [Fact]
    public void GenerateOtp_ReturnsSixDigitCode()
    {
        var logger = new TestLogger<OtpService>();
        var service = new OtpService(logger, CreateCache());

        var otp = service.GenerateOtp();

        Assert.Matches("^\\d{6}$", otp);
    }

    [Fact]
    public void ValidateOtp_DoesNotLogPlaintextOtp()
    {
        const string phone = "09123456789";
        const string code = "123456";

        var logger = new TestLogger<OtpService>();
        var service = new OtpService(logger, CreateCache());

        service.StoreOtp(phone, code);
        service.ValidateOtp(phone, code);

        Assert.DoesNotContain(logger.Messages, message => message.Contains(code, StringComparison.Ordinal));
    }


    [Fact]
    public void ValidateOtp_RejectsEmptyInputWithoutException()
    {
        const string phone = "09123456789";

        var logger = new TestLogger<OtpService>();
        var service = new OtpService(logger, CreateCache());

        service.StoreOtp(phone, "123456");
        var result = service.ValidateOtp(phone, " ");

        Assert.False(result.IsValid);
    }

    private static IMemoryCache CreateCache()
    {
        return new MemoryCache(new MemoryCacheOptions
        {
            SizeLimit = 10_000
        });
    }

}

internal sealed class TestLogger<T> : ILogger<T>
{
    public List<string> Messages { get; } = new();

    IDisposable ILogger.BeginScope<TState>(TState state) => NullScope.Instance;

    public bool IsEnabled(LogLevel logLevel) => true;

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception,
        Func<TState, Exception?, string> formatter)
    {
        Messages.Add(formatter(state, exception));
    }

    private sealed class NullScope : IDisposable
    {
        public static readonly NullScope Instance = new();
        public void Dispose() { }
    }
}

namespace PicoPlus.Application.Abstractions;

public interface IAuthSessionService
{
    Task<bool> IsLoggedInAsync(CancellationToken cancellationToken = default);
}

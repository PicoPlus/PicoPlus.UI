using PicoPlus.CleanArchitecture.Domain.Entities;
using PicoPlus.CleanArchitecture.Domain.ValueObjects;

namespace PicoPlus.CleanArchitecture.Application.Ports;

public interface IUserRepository
{
    Task<User?> FindByNationalCodeAsync(NationalCode nationalCode, CancellationToken cancellationToken = default);
}

using PicoPlus.CleanArchitecture.Application.DTOs;
using PicoPlus.CleanArchitecture.Application.UseCases.Auth;

namespace PicoPlus.CleanArchitecture.InterfaceAdapters.Auth;

public sealed class LoginViewModelAdapter
{
    private readonly ILoginByNationalCodeUseCase _useCase;

    public LoginViewModelAdapter(ILoginByNationalCodeUseCase useCase)
    {
        _useCase = useCase;
    }

    public Task<LoginByNationalCodeResult> LoginAsync(string nationalCode, string selectedRole, CancellationToken cancellationToken = default)
    {
        var request = new LoginByNationalCodeRequest(nationalCode, selectedRole);
        return _useCase.ExecuteAsync(request, cancellationToken);
    }
}

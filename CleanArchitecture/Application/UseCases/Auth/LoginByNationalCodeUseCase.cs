using PicoPlus.CleanArchitecture.Application.DTOs;
using PicoPlus.CleanArchitecture.Application.Ports;
using PicoPlus.CleanArchitecture.Domain.ValueObjects;

namespace PicoPlus.CleanArchitecture.Application.UseCases.Auth;

public interface ILoginByNationalCodeUseCase
{
    Task<LoginByNationalCodeResult> ExecuteAsync(LoginByNationalCodeRequest request, CancellationToken cancellationToken = default);
}

public sealed class LoginByNationalCodeUseCase : ILoginByNationalCodeUseCase
{
    private readonly IUserRepository _userRepository;
    private readonly IUserDataEnrichmentPort _enrichment;
    private readonly IAuthSessionPort _session;

    public LoginByNationalCodeUseCase(
        IUserRepository userRepository,
        IUserDataEnrichmentPort enrichment,
        IAuthSessionPort session)
    {
        _userRepository = userRepository;
        _enrichment = enrichment;
        _session = session;
    }

    public async Task<LoginByNationalCodeResult> ExecuteAsync(
        LoginByNationalCodeRequest request,
        CancellationToken cancellationToken = default)
    {
        NationalCode nationalCode;
        try
        {
            nationalCode = new NationalCode(request.NationalCode);
        }
        catch (ArgumentException ex)
        {
            return new LoginByNationalCodeResult(false, ex.Message, null, "/auth/login");
        }

        var existingUser = await _userRepository.FindByNationalCodeAsync(nationalCode, cancellationToken);
        if (existingUser is null)
        {
            return new LoginByNationalCodeResult(
                IsSuccess: false,
                ErrorMessage: "User was not found for this national code.",
                UserId: null,
                RedirectUri: "/auth/register",
                RequiresRegistration: true);
        }

        var enrichedUser = await _enrichment.EnrichAsync(existingUser, cancellationToken);
        await _session.StoreAuthenticatedUserAsync(enrichedUser, request.SelectedRole, cancellationToken);

        var redirectUri = request.SelectedRole == "Admin" ? "/admin/dashboard" : "/user";
        return new LoginByNationalCodeResult(true, null, enrichedUser.Id, redirectUri);
    }
}

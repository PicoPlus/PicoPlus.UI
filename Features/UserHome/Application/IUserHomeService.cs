using PicoPlus.Features.UserHome.Application.DTOs;
using PicoPlus.Models.CRM.Commerce;
using PicoPlus.Models.CRM.Objects;

namespace PicoPlus.Features.UserHome.Application;

public interface IUserHomeService
{
    Task<UserHomeDto?> InitializeAsync(CancellationToken cancellationToken = default);
    Task SignOutAsync(CancellationToken cancellationToken = default);
    Task ChangeMobileAsync(string newMobile, CancellationToken cancellationToken = default);
    Task CompleteBirthDateAsync(string birthDate, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<LineItem.Read.Response>> LoadDealLineItemsAsync(string dealId, CancellationToken cancellationToken = default);
    Task<UserHomeDto?> RefreshAsync(CancellationToken cancellationToken = default);
}

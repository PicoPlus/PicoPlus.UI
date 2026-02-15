using PicoPlus.Features.UserHome.Domain;
using PicoPlus.Models.CRM.Objects;

namespace PicoPlus.Features.UserHome.Application.DTOs;

public sealed record UserHomeDto(
    UserHomeProfile Profile,
    Contact.Search.Response.Result Contact,
    IReadOnlyList<UserDeal> Deals,
    UserHomeMetrics Metrics);

namespace PicoPlus.Features.UserHome.Domain;

public sealed record UserHomeMetrics(int TotalDeals, int ClosedDeals, int OpenDeals, decimal TotalRevenue, decimal WalletBalance);

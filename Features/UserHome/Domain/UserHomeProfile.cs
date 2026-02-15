namespace PicoPlus.Features.UserHome.Domain;

public sealed record UserHomeProfile(
    string ContactId,
    string FirstName,
    string LastName,
    string Phone,
    string NationalCode,
    string Email,
    string BirthDate,
    string FatherName,
    string Gender,
    string ShahkarStatus,
    string Wallet,
    string AvatarUrl)
{
    public string FullName => $"{FirstName} {LastName}".Trim();
    public string Initials => string.IsNullOrWhiteSpace(FirstName) || string.IsNullOrWhiteSpace(LastName)
        ? "U"
        : $"{FirstName[0]}{LastName[0]}";

    public decimal WalletAmount => decimal.TryParse(Wallet, out var value) ? value : 0m;
}

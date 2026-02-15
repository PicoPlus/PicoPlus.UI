namespace PicoPlus.CleanArchitecture.Domain.ValueObjects;

public readonly record struct NationalCode
{
    public string Value { get; }

    public NationalCode(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("National code is required.", nameof(value));

        var normalized = value.Trim();
        if (!IsValid(normalized))
            throw new ArgumentException("Invalid Iranian national code.", nameof(value));

        Value = normalized;
    }

    public static bool IsValid(string nationalCode)
    {
        if (nationalCode.Length != 10 || !nationalCode.All(char.IsDigit))
            return false;

        if (nationalCode.All(c => c == nationalCode[0]))
            return false;

        var sum = 0;
        for (var i = 0; i < 9; i++)
        {
            sum += (nationalCode[i] - '0') * (10 - i);
        }

        var remainder = sum % 11;
        var checkDigit = remainder < 2 ? remainder : 11 - remainder;
        return checkDigit == nationalCode[9] - '0';
    }

    public override string ToString() => Value;
}

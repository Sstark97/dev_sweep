using DevSweep.Domain.Common;
using DevSweep.Domain.Errors;

namespace DevSweep.Domain.ValueObjects;

public readonly record struct CleanupItemVersion : IComparable<CleanupItemVersion>
{
    private readonly Version value;

    private CleanupItemVersion(Version value) => this.value = value;

    public static Result<CleanupItemVersion, DomainError> Create(string rawVersion)
    {
        if (string.IsNullOrWhiteSpace(rawVersion))
            return Result<CleanupItemVersion, DomainError>.Failure(
                DomainError.Validation("Version string cannot be empty"));

        var parts = rawVersion.Split('.', StringSplitOptions.TrimEntries);

        if (parts.Length > 4 || parts.Any(part => string.IsNullOrEmpty(part) || part.Any(ch => !char.IsDigit(ch))))
            return Result<CleanupItemVersion, DomainError>.Failure(
                DomainError.Validation($"Invalid version format: '{rawVersion}'"));

        var numbers = parts.Select(int.Parse).ToArray();

        var parsed = numbers.Length switch
        {
            1 => new Version(numbers[0], 0),
            2 => new Version(numbers[0], numbers[1]),
            3 => new Version(numbers[0], numbers[1], numbers[2]),
            4 => new Version(numbers[0], numbers[1], numbers[2], numbers[3]),
            _ => new Version(0, 0)
        };

        return Result<CleanupItemVersion, DomainError>.Success(new CleanupItemVersion(parsed));
    }

    public int CompareTo(CleanupItemVersion other) => value.CompareTo(other.value);

    public static bool operator <(CleanupItemVersion left, CleanupItemVersion right) =>
        left.CompareTo(right) < 0;

    public static bool operator >(CleanupItemVersion left, CleanupItemVersion right) =>
        left.CompareTo(right) > 0;

    public static bool operator <=(CleanupItemVersion left, CleanupItemVersion right) =>
        left.CompareTo(right) <= 0;

    public static bool operator >=(CleanupItemVersion left, CleanupItemVersion right) =>
        left.CompareTo(right) >= 0;

    public override string ToString() => value.ToString();
}

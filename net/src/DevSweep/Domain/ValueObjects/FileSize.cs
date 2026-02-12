using System.Globalization;
using DevSweep.Domain.Common;
using DevSweep.Domain.Errors;

namespace DevSweep.Domain.ValueObjects;

public readonly record struct FileSize : IComparable<FileSize>
{
    private readonly long bytes;
    private const decimal BytesPerKilobyte = 1024m;

    private FileSize(long bytes) => this.bytes = bytes;

    public static Result<FileSize, DomainError> Create(long bytes)
    {
        if (bytes < 0)
            return Result<FileSize, DomainError>.Failure(
                DomainError.Validation("File size cannot be negative"));

        return Result<FileSize, DomainError>.Success(new FileSize(bytes));
    }

    public decimal InKilobytes() => bytes / BytesPerKilobyte;
    public decimal InMegabytes() => bytes / (BytesPerKilobyte * BytesPerKilobyte);
    public decimal InGigabytes() => bytes / (BytesPerKilobyte * BytesPerKilobyte * BytesPerKilobyte);

    public FileSize Add(FileSize other) => new(bytes + other.bytes);

    public int CompareTo(FileSize other) => bytes.CompareTo(other.bytes);

    public static bool operator <(FileSize left, FileSize right) => left.bytes < right.bytes;
    public static bool operator >(FileSize left, FileSize right) => left.bytes > right.bytes;
    public static bool operator <=(FileSize left, FileSize right) => left.bytes <= right.bytes;
    public static bool operator >=(FileSize left, FileSize right) => left.bytes >= right.bytes;

    public override string ToString()
    {
        return bytes switch
        {
            < 1024 => $"{bytes} B",
            < 1024 * 1024 => $"{InKilobytes().ToString("F2", CultureInfo.InvariantCulture)} KB",
            < 1024 * 1024 * 1024 => $"{InMegabytes().ToString("F2", CultureInfo.InvariantCulture)} MB",
            _ => $"{InGigabytes().ToString("F2", CultureInfo.InvariantCulture)} GB"
        };
    }
}

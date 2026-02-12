using DevSweep.Domain.Common;
using DevSweep.Domain.Errors;

namespace DevSweep.Domain.ValueObjects;

public readonly record struct FilePath
{
    private readonly string value;

    private FilePath(string value) => this.value = value;

    public static Result<FilePath, DomainError> Create(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
            return Result<FilePath, DomainError>.Failure(
                DomainError.Validation("File path cannot be empty"));

        if (path.Length > 260)
            return Result<FilePath, DomainError>.Failure(
                DomainError.Validation("File path exceeds maximum length of 260 characters"));

        return Result<FilePath, DomainError>.Success(new FilePath(path));
    }

    public string FileName() => Path.GetFileName(value);
    public string DirectoryPath() => Path.GetDirectoryName(value) ?? string.Empty;
    public string Extension() => Path.GetExtension(value);

    public override string ToString() => value;
}

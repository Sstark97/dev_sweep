using DevSweep.Domain.Common;
using DevSweep.Domain.Errors;
using DevSweep.Domain.ValueObjects;

namespace DevSweep.Application.Ports.Driven;

public interface IFileSystem
{
    bool DirectoryExists(FilePath path);
    bool FileExists(FilePath path);
    bool IsDirectoryNotEmpty(FilePath path);
    Result<FileSize, DomainError> Size(FilePath path);
    DateTime LastWriteTime(FilePath path);
    Task<Result<Unit, DomainError>> DeleteDirectoryAsync(FilePath path, CancellationToken cancellationToken);
    Task<Result<Unit, DomainError>> DeleteFileAsync(FilePath path, CancellationToken cancellationToken);
    Task<Result<IReadOnlyList<FilePath>, DomainError>> FindDirectoriesAsync(
        FilePath basePath, string pattern, CancellationToken cancellationToken);
    Task<Result<IReadOnlyList<FilePath>, DomainError>> FindFilesAsync(
        FilePath basePath, string pattern, CancellationToken cancellationToken);
}

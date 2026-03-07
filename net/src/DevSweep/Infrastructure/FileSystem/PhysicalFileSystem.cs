using System.Security;
using DevSweep.Application.Ports.Driven;
using DevSweep.Domain.Common;
using DevSweep.Domain.Errors;
using DevSweep.Domain.ValueObjects;

namespace DevSweep.Infrastructure.FileSystem;

public sealed class PhysicalFileSystem : IFileSystem
{
    public bool DirectoryExists(FilePath path) =>
        Directory.Exists(path.ToString());

    public bool FileExists(FilePath path) =>
        File.Exists(path.ToString());

    public bool IsDirectoryNotEmpty(FilePath path)
    {
        var location = path.ToString();
        return Directory.Exists(location) &&
               Directory.EnumerateFileSystemEntries(location).Any();
    }

    public Result<FileSize, DomainError> Size(FilePath path)
    {
        try
        {
            string pathName = path.ToString();
            if (File.Exists(pathName))
            {
                var fileInfo = new FileInfo(pathName);
                return FileSize.Create(fileInfo.Length);
            }

            if (Directory.Exists(pathName))
            {
                var directoryInfo = new DirectoryInfo(pathName);
                var totalBytes = (from file in directoryInfo.EnumerateFiles("*", SearchOption.AllDirectories)
                                  select file.Length).Sum();
                return FileSize.Create(totalBytes);
            }

            return Result<FileSize, DomainError>.Failure(
                DomainError.NotFound("Path", pathName));
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or SecurityException)
        {
            return Result<FileSize, DomainError>.Failure(
                DomainError.InvalidOperation(exception.Message));
        }
    }

    public DateTime LastWriteTime(FilePath path) =>
        File.GetLastWriteTimeUtc(path.ToString());

    public Task<Result<Unit, DomainError>> DeleteDirectoryAsync(
        FilePath path, CancellationToken cancellationToken) =>
        Task.Run(() =>
        {
            if (cancellationToken.IsCancellationRequested)
                return Result<Unit, DomainError>.Failure(
                    DomainError.InvalidOperation("Operation was cancelled"));

            try
            {
                Directory.Delete(path.ToString(), recursive: true);
                return Result<Unit, DomainError>.Success(Unit.Value);
            }
            catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or SecurityException)
            {
                return Result<Unit, DomainError>.Failure(
                    DomainError.InvalidOperation(exception.Message));
            }
        }, cancellationToken);

    public Task<Result<Unit, DomainError>> DeleteFileAsync(
        FilePath path, CancellationToken cancellationToken) =>
        Task.Run(() =>
        {
            if (cancellationToken.IsCancellationRequested)
                return Result<Unit, DomainError>.Failure(
                    DomainError.InvalidOperation("Operation was cancelled"));

            try
            {
                File.Delete(path.ToString());
                return Result<Unit, DomainError>.Success(Unit.Value);
            }
            catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or SecurityException)
            {
                return Result<Unit, DomainError>.Failure(
                    DomainError.InvalidOperation(exception.Message));
            }
        }, cancellationToken);

    public Task<Result<IReadOnlyList<FilePath>, DomainError>> FindDirectoriesAsync(
        FilePath basePath, string pattern, CancellationToken cancellationToken) =>
        Task.Run<Result<IReadOnlyList<FilePath>, DomainError>>(() =>
        {
            if (cancellationToken.IsCancellationRequested)
                return Result<IReadOnlyList<FilePath>, DomainError>.Failure(
                    DomainError.InvalidOperation("Operation was cancelled"));

            try
            {
                var entries = Directory.EnumerateDirectories(
                    basePath.ToString(), pattern, SearchOption.AllDirectories);

                IReadOnlyList<FilePath> paths = [.. from entry in entries
                    let filePathResult = FilePath.Create(entry)
                    where filePathResult.IsSuccess
                    select filePathResult.Value];

                return Result<IReadOnlyList<FilePath>, DomainError>.Success(paths);
            }
            catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or SecurityException)
            {
                return Result<IReadOnlyList<FilePath>, DomainError>.Failure(
                    DomainError.InvalidOperation(exception.Message));
            }
        }, cancellationToken);

    public Task<Result<IReadOnlyList<FilePath>, DomainError>> FindFilesAsync(
        FilePath basePath, string pattern, CancellationToken cancellationToken) =>
        Task.Run<Result<IReadOnlyList<FilePath>, DomainError>>(() =>
        {
            if (cancellationToken.IsCancellationRequested)
                return Result<IReadOnlyList<FilePath>, DomainError>.Failure(
                    DomainError.InvalidOperation("Operation was cancelled"));

            try
            {
                var entries = Directory.EnumerateFiles(
                    basePath.ToString(), pattern, SearchOption.AllDirectories);

                IReadOnlyList<FilePath> paths = [.. from entry in entries
                    let filePathResult = FilePath.Create(entry)
                    where filePathResult.IsSuccess
                    select filePathResult.Value];

                return Result<IReadOnlyList<FilePath>, DomainError>.Success(paths);
            }
            catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or SecurityException)
            {
                return Result<IReadOnlyList<FilePath>, DomainError>.Failure(
                    DomainError.InvalidOperation(exception.Message));
            }
        }, cancellationToken);
}

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
        RunSafeAsync(() =>
        {
            Directory.Delete(path.ToString(), recursive: true);
            return Result<Unit, DomainError>.Success(Unit.Value);
        }, cancellationToken);

    public Task<Result<Unit, DomainError>> DeleteFileAsync(
        FilePath path, CancellationToken cancellationToken) =>
        RunSafeAsync(() =>
        {
            File.Delete(path.ToString());
            return Result<Unit, DomainError>.Success(Unit.Value);
        }, cancellationToken);

    public Task<Result<IReadOnlyList<FilePath>, DomainError>> FindDirectoriesAsync(
        FilePath basePath, string pattern, CancellationToken cancellationToken) =>
        RunSafeAsync(
            () => FindEntries(basePath.ToString(), pattern, Directory.EnumerateDirectories),
            cancellationToken);

    public Task<Result<IReadOnlyList<FilePath>, DomainError>> FindFilesAsync(
        FilePath basePath, string pattern, CancellationToken cancellationToken) =>
        RunSafeAsync(
            () => FindEntries(basePath.ToString(), pattern, Directory.EnumerateFiles),
            cancellationToken);

    public Task<Result<IReadOnlyList<FilePath>, DomainError>> FindDirectoriesAsync(
        FilePath basePath, string directoryName, int maxDepth,
        IReadOnlyList<string> excludePatterns, CancellationToken cancellationToken) =>
        RunSafeAsync(
            () => WalkDirectories(basePath.ToString(), directoryName, maxDepth, excludePatterns, 0),
            cancellationToken);

    public Task<Result<DateTime, DomainError>> MostRecentWriteTimeAsync(
        FilePath directory, IReadOnlyList<string> excludeDirectoryNames,
        CancellationToken cancellationToken) =>
        RunSafeAsync(
            () => CollectMostRecentWriteTime(directory.ToString(), excludeDirectoryNames),
            cancellationToken);

    private static Task<Result<T, DomainError>> RunSafeAsync<T>(
        Func<Result<T, DomainError>> operation,
        CancellationToken cancellationToken) =>
        Task.Run(() =>
        {
            if (cancellationToken.IsCancellationRequested)
                return Result<T, DomainError>.Failure(
                    DomainError.InvalidOperation("Operation was cancelled"));

            try
            {
                return operation();
            }
            catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or SecurityException)
            {
                return Result<T, DomainError>.Failure(
                    DomainError.InvalidOperation(exception.Message));
            }
        }, cancellationToken);

    private static Result<IReadOnlyList<FilePath>, DomainError> FindEntries(
        string basePath,
        string pattern,
        Func<string, string, SearchOption, IEnumerable<string>> enumerator)
    {
        var entries = enumerator(basePath, pattern, SearchOption.AllDirectories);

        IReadOnlyList<FilePath> paths = [.. from entry in entries
            let filePathResult = FilePath.Create(entry)
            where filePathResult.IsSuccess
            select filePathResult.Value];

        return Result<IReadOnlyList<FilePath>, DomainError>.Success(paths);
    }

    private static Result<IReadOnlyList<FilePath>, DomainError> WalkDirectories(
        string currentPath, string targetName, int maxDepth,
        IReadOnlyList<string> excludePatterns, int currentDepth)
    {
        if (currentDepth > maxDepth)
            return Result<IReadOnlyList<FilePath>, DomainError>.Success([]);

        return TryEnumerateSubdirectories(currentPath)
            .Bind(subdirectories => GatherMatches(subdirectories, targetName, maxDepth, excludePatterns, currentDepth));
    }

    private static Result<IReadOnlyList<FilePath>, DomainError> GatherMatches(
        IEnumerable<string> subdirectories, string targetName, int maxDepth,
        IReadOnlyList<string> excludePatterns, int currentDepth)
    {
        var collected = new List<FilePath>();

        foreach (var subdirectory in subdirectories)
        {
            var name = Path.GetFileName(subdirectory);

            if (excludePatterns.Contains(name))
                continue;

            if (name == targetName)
            {
                var pathResult = FilePath.Create(subdirectory);
                if (pathResult.IsSuccess)
                    collected.Add(pathResult.Value);
                continue;
            }

            collected.AddRange(
                WalkDirectories(subdirectory, targetName, maxDepth, excludePatterns, currentDepth + 1)
                    .Recover([]).Value);
        }

        return Result<IReadOnlyList<FilePath>, DomainError>.Success(collected);
    }

    private static Result<IEnumerable<string>, DomainError> TryEnumerateSubdirectories(string currentPath)
    {
        try
        {
            return Result<IEnumerable<string>, DomainError>.Success(
                Directory.EnumerateDirectories(currentPath));
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or SecurityException)
        {
            return Result<IEnumerable<string>, DomainError>.Failure(
                DomainError.InvalidOperation($"Cannot enumerate directories in '{currentPath}': {exception.Message}"));
        }
    }

    private static Result<DateTime, DomainError> CollectMostRecentWriteTime(
        string directoryPath, IReadOnlyList<string> excludeDirectoryNames) =>
        TryEnumerateEntries(directoryPath)
            .Map(entries => MaxWriteTime(entries, excludeDirectoryNames));

    private static DateTime MaxWriteTime(
        IEnumerable<string> entries, IReadOnlyList<string> excludeDirectoryNames)
    {
        var times = from entry in entries
                    let time = Directory.Exists(entry)
                        ? MostRecentChildDirectoryWriteTime(entry, excludeDirectoryNames)
                        : MostRecentFileWriteTime(entry)
                    where time.IsSome
                    select time.ValueOr(DateTime.MinValue);

        return times.DefaultIfEmpty(DateTime.MinValue.ToUniversalTime()).Max();
    }

    private static Result<IEnumerable<string>, DomainError> TryEnumerateEntries(string directoryPath)
    {
        try
        {
            return Result<IEnumerable<string>, DomainError>.Success(
                Directory.EnumerateFileSystemEntries(directoryPath));
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or SecurityException)
        {
            return Result<IEnumerable<string>, DomainError>.Failure(
                DomainError.InvalidOperation($"Cannot enumerate entries in '{directoryPath}': {exception.Message}"));
        }
    }

    private static Option<DateTime> MostRecentFileWriteTime(string entry)
    {
        try
        {
            return Option<DateTime>.Some(File.GetLastWriteTimeUtc(entry));
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException or SecurityException)
        {
            return Option<DateTime>.None;
        }
    }

    private static Option<DateTime> MostRecentChildDirectoryWriteTime(
        string entry, IReadOnlyList<string> excludeDirectoryNames)
    {
        var dirName = Path.GetFileName(entry);
        if (excludeDirectoryNames.Contains(dirName))
            return Option<DateTime>.None;

        return Option<DateTime>.Some(
            CollectMostRecentWriteTime(entry, excludeDirectoryNames)
                .Recover(DateTime.MinValue.ToUniversalTime())
                .Value);
    }
}

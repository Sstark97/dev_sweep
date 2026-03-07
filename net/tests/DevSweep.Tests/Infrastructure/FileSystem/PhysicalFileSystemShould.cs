using AwesomeAssertions;
using DevSweep.Domain.ValueObjects;
using DevSweep.Infrastructure.FileSystem;

namespace DevSweep.Tests.Infrastructure.FileSystem;

internal sealed class PhysicalFileSystemShould : IDisposable
{
    private readonly string tempDirectory;
    private readonly PhysicalFileSystem fileSystem = new();

    public PhysicalFileSystemShould()
    {
        tempDirectory = Path.Combine(
            Path.GetTempPath(),
            "devsweep-test-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempDirectory);
    }

    public void Dispose()
    {
        if (Directory.Exists(tempDirectory))
            Directory.Delete(tempDirectory, recursive: true);
    }

    [Test]
    public void DetectExistingDirectory()
    {
        var existingDir = CreateTestDirectory("existing");
        var path = FilePath.Create(existingDir).Value;

        var exists = fileSystem.DirectoryExists(path);

        exists.Should().BeTrue();
    }

    [Test]
    public void NotDetectMissingDirectory()
    {
        var missingPath = Path.Combine(tempDirectory, "missing-dir");
        var path = FilePath.Create(missingPath).Value;

        var exists = fileSystem.DirectoryExists(path);

        exists.Should().BeFalse();
    }

    [Test]
    public void DetectExistingFile()
    {
        var existingFile = CreateTestFile("existing.txt", [1, 2, 3]);
        var path = FilePath.Create(existingFile).Value;

        var exists = fileSystem.FileExists(path);

        exists.Should().BeTrue();
    }

    [Test]
    public void NotDetectMissingFile()
    {
        var missingPath = Path.Combine(tempDirectory, "missing-file.txt");
        var path = FilePath.Create(missingPath).Value;

        var exists = fileSystem.FileExists(path);

        exists.Should().BeFalse();
    }

    [Test]
    public void KnowDirectoryIsNotEmptyWhenItHasFiles()
    {
        var populatedDir = CreateTestDirectory("populated");
        File.WriteAllBytes(Path.Combine(populatedDir, "file.txt"), [10, 20, 30]);
        var path = FilePath.Create(populatedDir).Value;

        var notEmpty = fileSystem.IsDirectoryNotEmpty(path);

        notEmpty.Should().BeTrue();
    }

    [Test]
    public void KnowDirectoryIsEmptyWhenItHasNoFiles()
    {
        var emptyDir = CreateTestDirectory("empty");
        var path = FilePath.Create(emptyDir).Value;

        var notEmpty = fileSystem.IsDirectoryNotEmpty(path);

        notEmpty.Should().BeFalse();
    }

    [Test]
    public void TreatMissingDirectoryAsEmpty()
    {
        var missingPath = Path.Combine(tempDirectory, "nonexistent");
        var path = FilePath.Create(missingPath).Value;

        var notEmpty = fileSystem.IsDirectoryNotEmpty(path);

        notEmpty.Should().BeFalse();
    }

    [Test]
    public void CalculateSizeForSingleFile()
    {
        var knownContent = new byte[512];
        var testFile = CreateTestFile("sized-file.bin", knownContent);
        var path = FilePath.Create(testFile).Value;

        var result = fileSystem.Size(path);

        var size = result.Value;
        result.IsSuccess.Should().BeTrue();
        size.InKilobytes().Should().Be(0.5m);
    }

    [Test]
    public void CalculateSizeRecursivelyForDirectory()
    {
        var rootDir = CreateTestDirectory("sized-root");
        var subDir = Path.Combine(rootDir, "sub");
        Directory.CreateDirectory(subDir);

        var firstContent = new byte[256];
        var secondContent = new byte[512];
        File.WriteAllBytes(Path.Combine(rootDir, "first.bin"), firstContent);
        File.WriteAllBytes(Path.Combine(subDir, "second.bin"), secondContent);

        var path = FilePath.Create(rootDir).Value;
        var expectedBytes = firstContent.Length + secondContent.Length;

        var result = fileSystem.Size(path);

        var size = result.Value;
        result.IsSuccess.Should().BeTrue();
        size.InKilobytes().Should().Be(expectedBytes / 1024m);
    }

    [Test]
    public void FailWhenPathDoesNotExist()
    {
        var missingPath = Path.Combine(tempDirectory, "ghost-path");
        var path = FilePath.Create(missingPath).Value;

        var result = fileSystem.Size(path);

        result.IsFailure.Should().BeTrue();
        result.Error.IsNotFoundError().Should().BeTrue();
    }

    [Test]
    public void KnowWhenFileWasLastWritten()
    {
        var beforeWrite = DateTime.UtcNow.AddSeconds(-1);
        var testFile = CreateTestFile("timed-file.txt", [1, 2, 3]);
        var path = FilePath.Create(testFile).Value;

        var lastWrite = fileSystem.LastWriteTime(path);

        lastWrite.Should().BeAfter(beforeWrite);
    }

    [Test]
    public async Task DeleteExistingDirectorySuccessfully()
    {
        var dirToDelete = CreateTestDirectory("to-delete");
        var path = FilePath.Create(dirToDelete).Value;

        var result = await fileSystem.DeleteDirectoryAsync(path, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        Directory.Exists(dirToDelete).Should().BeFalse();
    }

    [Test]
    public async Task FailWhenDirectoryDoesNotExist()
    {
        var missingPath = Path.Combine(tempDirectory, "nonexistent-dir");
        var path = FilePath.Create(missingPath).Value;

        var result = await fileSystem.DeleteDirectoryAsync(path, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.IsInvalidOperationError().Should().BeTrue();
    }

    [Test]
    public async Task DeleteExistingFileSuccessfully()
    {
        var fileToDelete = CreateTestFile("to-delete.txt", [1, 2, 3]);
        var path = FilePath.Create(fileToDelete).Value;

        var result = await fileSystem.DeleteFileAsync(path, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        File.Exists(fileToDelete).Should().BeFalse();
    }

    [Test]
    public async Task SucceedWhenDeletingNonExistentFile()
    {
        var missingPath = Path.Combine(tempDirectory, "nonexistent.txt");
        var path = FilePath.Create(missingPath).Value;

        var result = await fileSystem.DeleteFileAsync(path, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
    }

    [Test]
    public async Task FindDirectoriesMatchingPattern()
    {
        var rootDir = CreateTestDirectory("find-dirs-root");
        Directory.CreateDirectory(Path.Combine(rootDir, "cache"));
        Directory.CreateDirectory(Path.Combine(rootDir, "logs"));
        var basePath = FilePath.Create(rootDir).Value;

        var result = await fileSystem.FindDirectoriesAsync(basePath, "cache", CancellationToken.None);

        var directories = result.Value;
        result.IsSuccess.Should().BeTrue();
        directories.Should().HaveCount(1);
        directories[0].ToString().Should().EndWith("cache");
    }

    [Test]
    public async Task FindNoDirectoriesWhenNoneMatchPattern()
    {
        var rootDir = CreateTestDirectory("find-dirs-empty-root");
        var basePath = FilePath.Create(rootDir).Value;

        var result = await fileSystem.FindDirectoriesAsync(basePath, "cache", CancellationToken.None);

        var directories = result.Value;
        result.IsSuccess.Should().BeTrue();
        directories.Should().BeEmpty();
    }

    [Test]
    public async Task FindFilesMatchingPattern()
    {
        var rootDir = CreateTestDirectory("find-files-root");
        File.WriteAllBytes(Path.Combine(rootDir, "report.log"), [1, 2]);
        File.WriteAllBytes(Path.Combine(rootDir, "debug.txt"), [3, 4]);
        var basePath = FilePath.Create(rootDir).Value;

        var result = await fileSystem.FindFilesAsync(basePath, "*.log", CancellationToken.None);

        var files = result.Value;
        result.IsSuccess.Should().BeTrue();
        files.Should().HaveCount(1);
        files[0].ToString().Should().EndWith("report.log");
    }

    [Test]
    public async Task FindNoFilesWhenNoneMatchPattern()
    {
        var rootDir = CreateTestDirectory("find-files-empty-root");
        var basePath = FilePath.Create(rootDir).Value;

        var result = await fileSystem.FindFilesAsync(basePath, "*.log", CancellationToken.None);

        var files = result.Value;
        result.IsSuccess.Should().BeTrue();
        files.Should().BeEmpty();
    }

    [Test]
    public async Task FindFilesInNestedDirectories()
    {
        var rootDir = CreateTestDirectory("find-files-nested-root");
        var subDir = Path.Combine(rootDir, "sub");
        var deepDir = Path.Combine(subDir, "deep");
        Directory.CreateDirectory(subDir);
        Directory.CreateDirectory(deepDir);
        File.WriteAllBytes(Path.Combine(rootDir, "top.log"), [1]);
        File.WriteAllBytes(Path.Combine(subDir, "mid.log"), [2]);
        File.WriteAllBytes(Path.Combine(deepDir, "deep.log"), [3]);
        var basePath = FilePath.Create(rootDir).Value;

        var result = await fileSystem.FindFilesAsync(basePath, "*.log", CancellationToken.None);

        var files = result.Value;
        result.IsSuccess.Should().BeTrue();
        files.Should().HaveCount(3);
    }

    private string CreateTestDirectory(string name)
    {
        var dirPath = Path.Combine(tempDirectory, name);
        Directory.CreateDirectory(dirPath);
        return dirPath;
    }

    private string CreateTestFile(string name, byte[] content)
    {
        var filePath = Path.Combine(tempDirectory, name);
        File.WriteAllBytes(filePath, content);
        return filePath;
    }
}

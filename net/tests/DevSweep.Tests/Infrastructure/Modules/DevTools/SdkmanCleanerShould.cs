using AwesomeAssertions;
using DevSweep.Application.Ports.Driven;
using DevSweep.Domain.Common;
using DevSweep.Domain.Enums;
using DevSweep.Domain.Errors;
using DevSweep.Domain.ValueObjects;
using DevSweep.Infrastructure.Modules.DevTools;
using DevSweep.Tests.Builders;
using NSubstitute;

namespace DevSweep.Tests.Infrastructure.Modules.DevTools;

internal sealed class SdkmanCleanerShould
{
    private readonly IFileSystem fileSystem = Substitute.For<IFileSystem>();
    private readonly IEnvironmentProvider environment = Substitute.For<IEnvironmentProvider>();
    private readonly SdkmanCleaner cleaner;

    private static readonly FilePath SdkmanPath = FilePath.Create(Path.Combine("any", "home", ".sdkman", "tmp")).Value;

    public SdkmanCleanerShould()
    {
        cleaner = new SdkmanCleaner(fileSystem, environment);
        environment.SdkmanPath().Returns(SdkmanPath);
    }

    [Test]
    public void BeAvailableWhenPlatformIsMacOs()
    {
        cleaner.IsAvailable(OperatingSystemType.MacOS).Should().BeTrue();
    }

    [Test]
    public void BeAvailableWhenPlatformIsLinux()
    {
        cleaner.IsAvailable(OperatingSystemType.Linux).Should().BeTrue();
    }

    [Test]
    public void BeUnavailableWhenPlatformIsWindows()
    {
        cleaner.IsAvailable(OperatingSystemType.Windows).Should().BeFalse();
    }

    [Test]
    public async Task FindSdkmanTmpWhenDirectoryIsNonEmpty()
    {
        var largeSize = new CleanableItemBuilder().Large().Build().Size;
        GivenSdkmanDirectoryIsNonEmpty(largeSize);

        var result = await cleaner.AnalyzeAsync(CancellationToken.None);

        var items = result.Value;
        result.IsSuccess.Should().BeTrue();
        items.Should().Contain(i => i.Reason == "devtools:sdkman-tmp");
    }

    [Test]
    public async Task FindNoItemsWhenSdkmanDirectoryMissing()
    {
        GivenSdkmanDirectoryMissing();

        var result = await cleaner.AnalyzeAsync(CancellationToken.None);

        var items = result.Value;
        result.IsSuccess.Should().BeTrue();
        items.Should().BeEmpty();
    }

    [Test]
    public async Task FindNoItemsWhenSdkmanDirectoryIsEmpty()
    {
        fileSystem.DirectoryExists(SdkmanPath).Returns(true);
        fileSystem.IsDirectoryNotEmpty(SdkmanPath).Returns(false);

        var result = await cleaner.AnalyzeAsync(CancellationToken.None);

        var items = result.Value;
        result.IsSuccess.Should().BeTrue();
        items.Should().BeEmpty();
    }

    [Test]
    public async Task DeleteSdkmanTmpOnCleanup()
    {
        var sdkmanItem = new CleanableItemBuilder()
            .ForModule(CleanupModuleName.DevTools)
            .WithReason("devtools:sdkman-tmp")
            .WithPath(Path.Combine("any", "home", ".sdkman", "tmp"))
            .Large()
            .Build();
        GivenDeleteSucceeds(sdkmanItem.Path);

        var result = await cleaner.CleanAsync([sdkmanItem], CancellationToken.None);

        var cleanupResult = result.Value;
        result.IsSuccess.Should().BeTrue();
        cleanupResult.TotalFilesDeleted().Should().Be(1);
        cleanupResult.TotalSpaceFreed().Should().Be(sdkmanItem.Size);
    }

    [Test]
    public async Task RecordErrorWhenDeleteFails()
    {
        var sdkmanItem = new CleanableItemBuilder()
            .ForModule(CleanupModuleName.DevTools)
            .WithReason("devtools:sdkman-tmp")
            .WithPath(Path.Combine("any", "home", ".sdkman", "tmp"))
            .Large()
            .Build();
        GivenDeleteFails(sdkmanItem.Path);

        var result = await cleaner.CleanAsync([sdkmanItem], CancellationToken.None);

        var cleanupResult = result.Value;
        result.IsSuccess.Should().BeTrue();
        cleanupResult.HasErrors().Should().BeTrue();
        cleanupResult.TotalFilesDeleted().Should().Be(0);
    }

    private void GivenSdkmanDirectoryIsNonEmpty(FileSize size)
    {
        fileSystem.DirectoryExists(SdkmanPath).Returns(true);
        fileSystem.IsDirectoryNotEmpty(SdkmanPath).Returns(true);
        fileSystem.Size(SdkmanPath).Returns(Result<FileSize, DomainError>.Success(size));
    }

    private void GivenSdkmanDirectoryMissing() =>
        fileSystem.DirectoryExists(SdkmanPath).Returns(false);

    private void GivenDeleteSucceeds(FilePath path) =>
        fileSystem.DeleteDirectoryAsync(path, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Result<Unit, DomainError>.Success(Unit.Value)));

    private void GivenDeleteFails(FilePath path) =>
        fileSystem.DeleteDirectoryAsync(path, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Result<Unit, DomainError>.Failure(
                DomainError.InvalidOperation("Permission denied"))));
}

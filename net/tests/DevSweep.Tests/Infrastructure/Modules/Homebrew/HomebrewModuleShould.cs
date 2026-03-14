using AwesomeAssertions;
using DevSweep.Application.Models;
using DevSweep.Application.Ports.Driven;
using DevSweep.Domain.Common;
using DevSweep.Domain.Enums;
using DevSweep.Domain.Errors;
using DevSweep.Domain.ValueObjects;
using DevSweep.Infrastructure.Modules.Homebrew;
using DevSweep.Tests.Builders;
using NSubstitute;

namespace DevSweep.Tests.Infrastructure.Modules.Homebrew;

internal sealed class HomebrewModuleShould
{
    private readonly ICommandRunner commandRunner = Substitute.For<ICommandRunner>();
    private readonly IFileSystem fileSystem = Substitute.For<IFileSystem>();
    private readonly IEnvironmentProvider environment = Substitute.For<IEnvironmentProvider>();
    private readonly HomebrewModule module;

    private static readonly FilePath CachePath = FilePath.Create(Path.Combine("any", "homebrew", "cache")).Value;

    public HomebrewModuleShould()
    {
        module = new HomebrewModule(commandRunner, fileSystem, environment);
        environment.HomebrewCachePath().Returns(CachePath);
    }

    // --- Platform availability ---

    [Test]
    public void BeAvailableWhenPlatformIsMacOs()
    {
        module.IsAvailableOnPlatform(OperatingSystemType.MacOS).Should().BeTrue();
    }

    [Test]
    public void BeAvailableWhenPlatformIsLinux()
    {
        module.IsAvailableOnPlatform(OperatingSystemType.Linux).Should().BeTrue();
    }

    [Test]
    public void BeUnavailableWhenPlatformIsWindows()
    {
        module.IsAvailableOnPlatform(OperatingSystemType.Windows).Should().BeFalse();
    }

    [Test]
    public void BeUnavailableWhenPlatformIsUnknown()
    {
        module.IsAvailableOnPlatform((OperatingSystemType)999).Should().BeFalse();
    }

    [Test]
    public void MarkOperationAsDestructive()
    {
        module.IsDestructive.Should().BeTrue();
    }

    // --- Analysis: brew not available ---

    [Test]
    public async Task FindNoItemsWhenBrewCliMissing()
    {
        GivenBrewNotAvailable();

        var result = await module.AnalyzeAsync(CancellationToken.None);

        var analysis = result.Value;
        result.IsSuccess.Should().BeTrue();
        analysis.ItemCount().Should().Be(0);
    }

    // --- Analysis: download cache ---

    [Test]
    public async Task IncludeDownloadCacheWhenCacheDirectoryIsNonEmpty()
    {
        var largeSize = new CleanableItemBuilder().Large().Build().Size;
        GivenBrewAvailable();
        GivenCacheDirectoryIsNonEmpty(largeSize);
        GivenNoOutdatedPackages();
        GivenNoUnusedDeps();

        var result = await module.AnalyzeAsync(CancellationToken.None);

        var items = result.Value.Items();
        result.IsSuccess.Should().BeTrue();
        items.Should().Contain(i => i.Reason == "homebrew:download-cache");
    }

    [Test]
    public async Task SkipDownloadCacheWhenCacheDirectoryMissing()
    {
        GivenBrewAvailable();
        GivenNoAnalysisSources();

        var result = await module.AnalyzeAsync(CancellationToken.None);

        var items = result.Value.Items();
        result.IsSuccess.Should().BeTrue();
        items.Should().NotContain(i => i.Reason == "homebrew:download-cache");
    }

    [Test]
    public async Task SkipDownloadCacheWhenCacheDirectoryIsEmpty()
    {
        GivenBrewAvailable();
        fileSystem.DirectoryExists(CachePath).Returns(true);
        fileSystem.IsDirectoryNotEmpty(CachePath).Returns(false);
        GivenNoOutdatedPackages();
        GivenNoUnusedDeps();

        var result = await module.AnalyzeAsync(CancellationToken.None);

        var items = result.Value.Items();
        result.IsSuccess.Should().BeTrue();
        items.Should().NotContain(i => i.Reason == "homebrew:download-cache");
    }

    // --- Analysis: outdated packages ---

    [Test]
    public async Task IncludeOutdatedPackagesItemWhenDryRunReportsPackages()
    {
        GivenBrewAvailable();
        GivenCacheDirectoryMissing();
        GivenOutdatedPackageCount(3);
        GivenNoUnusedDeps();

        var result = await module.AnalyzeAsync(CancellationToken.None);

        var items = result.Value.Items();
        result.IsSuccess.Should().BeTrue();
        items.Should().Contain(i => i.Reason == "homebrew:outdated-packages");
    }

    [Test]
    public async Task SkipOutdatedPackagesWhenDryRunReportsNone()
    {
        GivenBrewAvailable();
        GivenNoAnalysisSources();

        var result = await module.AnalyzeAsync(CancellationToken.None);

        var items = result.Value.Items();
        result.IsSuccess.Should().BeTrue();
        items.Should().NotContain(i => i.Reason == "homebrew:outdated-packages");
    }

    [Test]
    public async Task SkipOutdatedPackagesWhenDryRunCommandFails()
    {
        GivenBrewAvailable();
        GivenCacheDirectoryMissing();
        GivenBrewCommandFails("cleanup --dry-run");
        GivenNoUnusedDeps();

        var result = await module.AnalyzeAsync(CancellationToken.None);

        var items = result.Value.Items();
        result.IsSuccess.Should().BeTrue();
        items.Should().NotContain(i => i.Reason == "homebrew:outdated-packages");
    }

    // --- Analysis: unused dependencies ---

    [Test]
    public async Task IncludeUnusedDepsItemWhenDryRunReportsRemovable()
    {
        GivenBrewAvailable();
        GivenCacheDirectoryMissing();
        GivenNoOutdatedPackages();
        GivenUnusedDepsCount(2);

        var result = await module.AnalyzeAsync(CancellationToken.None);

        var items = result.Value.Items();
        result.IsSuccess.Should().BeTrue();
        items.Should().Contain(i => i.Reason == "homebrew:unused-deps");
    }

    [Test]
    public async Task SkipUnusedDepsWhenDryRunReportsNone()
    {
        GivenBrewAvailable();
        GivenNoAnalysisSources();

        var result = await module.AnalyzeAsync(CancellationToken.None);

        var items = result.Value.Items();
        result.IsSuccess.Should().BeTrue();
        items.Should().NotContain(i => i.Reason == "homebrew:unused-deps");
    }

    [Test]
    public async Task SkipUnusedDepsWhenDryRunCommandFails()
    {
        GivenBrewAvailable();
        GivenCacheDirectoryMissing();
        GivenNoOutdatedPackages();
        GivenBrewCommandFails("autoremove --dry-run");

        var result = await module.AnalyzeAsync(CancellationToken.None);

        var items = result.Value.Items();
        result.IsSuccess.Should().BeTrue();
        items.Should().NotContain(i => i.Reason == "homebrew:unused-deps");
    }

    // --- Analysis: combined ---

    [Test]
    public async Task IncludeAllCandidatesWhenAllSourcesReportItems()
    {
        var largeSize = new CleanableItemBuilder().Large().Build().Size;
        GivenBrewAvailable();
        GivenCacheDirectoryIsNonEmpty(largeSize);
        GivenOutdatedPackageCount(2);
        GivenUnusedDepsCount(3);

        var result = await module.AnalyzeAsync(CancellationToken.None);

        var analysis = result.Value;
        result.IsSuccess.Should().BeTrue();
        analysis.ItemCount().Should().Be(3);
    }

    // --- Cleanup: empty/unavailable ---

    [Test]
    public async Task CleanNothingWhenItemListIsEmpty()
    {
        var result = await module.CleanAsync([], CancellationToken.None);

        var cleanupResult = result.Value;
        result.IsSuccess.Should().BeTrue();
        cleanupResult.TotalFilesDeleted().Should().Be(0);
    }

    [Test]
    public async Task CleanNothingWhenBrewCliMissing()
    {
        var item = new CleanableItemBuilder()
            .ForModule(CleanupModuleName.Homebrew)
            .WithReason("homebrew:download-cache")
            .Large()
            .Build();
        GivenBrewNotAvailable();

        var result = await module.CleanAsync([item], CancellationToken.None);

        var cleanupResult = result.Value;
        result.IsSuccess.Should().BeTrue();
        cleanupResult.TotalFilesDeleted().Should().Be(0);
    }

    // --- Cleanup: success paths ---

    [Test]
    public async Task ExecuteBrewCleanupWhenOutdatedPackagesItemProvided()
    {
        var outdatedItem = new CleanableItemBuilder()
            .ForModule(CleanupModuleName.Homebrew)
            .WithReason("homebrew:outdated-packages")
            .Large()
            .Build();
        GivenBrewAvailable();
        GivenBrewCommandSucceeds("cleanup --prune=all");

        await module.CleanAsync([outdatedItem], CancellationToken.None);

        await commandRunner.Received(1).RunAsync("brew", "cleanup --prune=all", Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task ExecuteBrewAutoremoveWhenUnusedDepsItemProvided()
    {
        var unusedDepsItem = new CleanableItemBuilder()
            .ForModule(CleanupModuleName.Homebrew)
            .WithReason("homebrew:unused-deps")
            .Large()
            .Build();
        GivenBrewAvailable();
        GivenBrewCommandSucceeds("autoremove");

        await module.CleanAsync([unusedDepsItem], CancellationToken.None);

        await commandRunner.Received(1).RunAsync("brew", "autoremove", Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task TrackFreedSpaceAfterSuccessfulCleanup()
    {
        var cacheItem = new CleanableItemBuilder()
            .ForModule(CleanupModuleName.Homebrew)
            .WithReason("homebrew:download-cache")
            .Large()
            .Build();
        GivenBrewAvailable();
        GivenBrewCommandSucceeds("cleanup --prune=all");

        var result = await module.CleanAsync([cacheItem], CancellationToken.None);

        var cleanupResult = result.Value;
        result.IsSuccess.Should().BeTrue();
        cleanupResult.TotalFilesDeleted().Should().Be(1);
        cleanupResult.TotalSpaceFreed().Should().Be(cacheItem.Size);
    }

    [Test]
    public async Task AccumulateFreedSpaceAcrossMultipleCleanOperations()
    {
        var cacheItem = new CleanableItemBuilder()
            .ForModule(CleanupModuleName.Homebrew)
            .WithReason("homebrew:download-cache")
            .Small()
            .Build();
        var unusedDepsItem = new CleanableItemBuilder()
            .ForModule(CleanupModuleName.Homebrew)
            .WithReason("homebrew:unused-deps")
            .Large()
            .Build();
        var expectedTotal = cacheItem.Size.Add(unusedDepsItem.Size);
        GivenBrewAvailable();
        GivenBrewCommandSucceeds("cleanup --prune=all");
        GivenBrewCommandSucceeds("autoremove");

        var result = await module.CleanAsync([cacheItem, unusedDepsItem], CancellationToken.None);

        var cleanupResult = result.Value;
        result.IsSuccess.Should().BeTrue();
        cleanupResult.TotalFilesDeleted().Should().Be(2);
        cleanupResult.TotalSpaceFreed().Should().Be(expectedTotal);
    }

    // --- Cleanup: failure paths ---

    [Test]
    public async Task RecordErrorWhenBrewCleanupCommandFails()
    {
        var outdatedItem = new CleanableItemBuilder()
            .ForModule(CleanupModuleName.Homebrew)
            .WithReason("homebrew:outdated-packages")
            .Large()
            .Build();
        GivenBrewAvailable();
        GivenBrewCommandFails("cleanup --prune=all");

        var result = await module.CleanAsync([outdatedItem], CancellationToken.None);

        var cleanupResult = result.Value;
        result.IsSuccess.Should().BeTrue();
        cleanupResult.HasErrors().Should().BeTrue();
        cleanupResult.TotalFilesDeleted().Should().Be(0);
    }

    [Test]
    public async Task RecordErrorWhenBrewAutoremoveCommandFails()
    {
        var unusedDepsItem = new CleanableItemBuilder()
            .ForModule(CleanupModuleName.Homebrew)
            .WithReason("homebrew:unused-deps")
            .Large()
            .Build();
        GivenBrewAvailable();
        GivenBrewCommandFails("autoremove");

        var result = await module.CleanAsync([unusedDepsItem], CancellationToken.None);

        var cleanupResult = result.Value;
        result.IsSuccess.Should().BeTrue();
        cleanupResult.HasErrors().Should().BeTrue();
        cleanupResult.TotalFilesDeleted().Should().Be(0);
    }

    [Test]
    public async Task ContinueCleaningWhenOneCommandFails()
    {
        var cacheItem = new CleanableItemBuilder()
            .ForModule(CleanupModuleName.Homebrew)
            .WithReason("homebrew:download-cache")
            .Small()
            .Build();
        var unusedDepsItem = new CleanableItemBuilder()
            .ForModule(CleanupModuleName.Homebrew)
            .WithReason("homebrew:unused-deps")
            .Large()
            .Build();
        GivenBrewAvailable();
        GivenBrewCommandFails("cleanup --prune=all");
        GivenBrewCommandSucceeds("autoremove");

        var result = await module.CleanAsync([cacheItem, unusedDepsItem], CancellationToken.None);

        var cleanupResult = result.Value;
        result.IsSuccess.Should().BeTrue();
        cleanupResult.HasErrors().Should().BeTrue();
        cleanupResult.TotalFilesDeleted().Should().Be(1);
    }

    // --- Private Given helpers ---

    private void GivenBrewAvailable() =>
        commandRunner.IsCommandAvailable("brew").Returns(true);

    private void GivenBrewNotAvailable() =>
        commandRunner.IsCommandAvailable("brew").Returns(false);

    private void GivenCacheDirectoryIsNonEmpty(FileSize size)
    {
        fileSystem.DirectoryExists(CachePath).Returns(true);
        fileSystem.IsDirectoryNotEmpty(CachePath).Returns(true);
        fileSystem.Size(CachePath).Returns(Result<FileSize, DomainError>.Success(size));
    }

    private void GivenCacheDirectoryMissing() =>
        fileSystem.DirectoryExists(CachePath).Returns(false);

    private void GivenOutdatedPackageCount(int count)
    {
        var output = string.Join("\n", Enumerable.Repeat("Would remove: formula", count));
        commandRunner.RunAsync("brew", "cleanup --dry-run", Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(CommandOutput.Create(0, output, string.Empty)));
    }

    private void GivenNoOutdatedPackages() =>
        commandRunner.RunAsync("brew", "cleanup --dry-run", Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(CommandOutput.Create(0, string.Empty, string.Empty)));

    private void GivenUnusedDepsCount(int count)
    {
        var output = string.Join("\n", Enumerable.Repeat("Would remove: some-dep", count));
        commandRunner.RunAsync("brew", "autoremove --dry-run", Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(CommandOutput.Create(0, output, string.Empty)));
    }

    private void GivenNoUnusedDeps() =>
        commandRunner.RunAsync("brew", "autoremove --dry-run", Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(CommandOutput.Create(0, "Nothing to remove", string.Empty)));

    private void GivenBrewCommandSucceeds(string arguments) =>
        commandRunner.RunAsync("brew", arguments, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(CommandOutput.Create(0, "Successfully cleaned", string.Empty)));

    private void GivenBrewCommandFails(string arguments) =>
        commandRunner.RunAsync("brew", arguments, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(CommandOutput.Create(1, string.Empty, "Error: command failed")));

    private void GivenNoAnalysisSources()
    {
        GivenCacheDirectoryMissing();
        GivenNoOutdatedPackages();
        GivenNoUnusedDeps();
    }
}

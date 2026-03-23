using AwesomeAssertions;
using DevSweep.Application.Ports.Driven;
using DevSweep.Domain.Common;
using DevSweep.Domain.Entities;
using DevSweep.Domain.Enums;
using DevSweep.Domain.Errors;
using DevSweep.Domain.ValueObjects;
using DevSweep.Infrastructure.Modules.System;
using DevSweep.Tests.Builders;
using NSubstitute;

namespace DevSweep.Tests.Infrastructure.Modules.System;

internal sealed class SystemModuleShould
{
    private readonly ISystemCleaner cacheCleaner = Substitute.For<ISystemCleaner>();
    private readonly ISystemCleaner logsCleaner = Substitute.For<ISystemCleaner>();
    private readonly IEnvironmentProvider environment = Substitute.For<IEnvironmentProvider>();
    private readonly SystemModule module;

    public SystemModuleShould()
    {
        environment.CurrentOperatingSystem.Returns(OperatingSystemType.MacOS);

        cacheCleaner.CleanerName.Returns("SystemCache");
        cacheCleaner.IsAvailable(Arg.Any<OperatingSystemType>()).Returns(true);
        cacheCleaner.AnalyzeAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Result<IReadOnlyList<CleanableItem>, DomainError>.Success([])));
        cacheCleaner.CleanAsync(Arg.Any<IReadOnlyList<CleanableItem>>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Result<CleanupResult, DomainError>.Success(CleanupResult.Empty)));

        logsCleaner.CleanerName.Returns("SystemLogs");
        logsCleaner.IsAvailable(Arg.Any<OperatingSystemType>()).Returns(true);
        logsCleaner.AnalyzeAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Result<IReadOnlyList<CleanableItem>, DomainError>.Success([])));
        logsCleaner.CleanAsync(Arg.Any<IReadOnlyList<CleanableItem>>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Result<CleanupResult, DomainError>.Success(CleanupResult.Empty)));

        module = new SystemModule(environment, [cacheCleaner, logsCleaner]);
    }

    [Test]
    public void BeNamedSystem()
    {
        module.Name.Should().Be(CleanupModuleName.System);
    }

    [Test]
    public void BeAvailableOnAllPlatforms()
    {
        module.IsAvailableOnPlatform(OperatingSystemType.MacOS).Should().BeTrue();
        module.IsAvailableOnPlatform(OperatingSystemType.Linux).Should().BeTrue();
        module.IsAvailableOnPlatform(OperatingSystemType.Windows).Should().BeTrue();
    }

    [Test]
    public void NotBeDestructive()
    {
        module.IsDestructive.Should().BeFalse();
    }

    [Test]
    public async Task AggregateItemsFromAllCleaners()
    {
        var cacheItem = new CleanableItemBuilder()
            .ForModule(CleanupModuleName.System)
            .WithReason("system:user-cache")
            .Large()
            .Build();
        var logsItem = new CleanableItemBuilder()
            .ForModule(CleanupModuleName.System)
            .WithReason("system:user-logs")
            .Small()
            .Build();
        cacheCleaner.AnalyzeAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Result<IReadOnlyList<CleanableItem>, DomainError>.Success([cacheItem])));
        logsCleaner.AnalyzeAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Result<IReadOnlyList<CleanableItem>, DomainError>.Success([logsItem])));

        var result = await module.AnalyzeAsync(CancellationToken.None);

        var analysis = result.Value;
        result.IsSuccess.Should().BeTrue();
        analysis.ItemCount().Should().Be(2);
    }

    [Test]
    public async Task SkipUnavailableCleaners()
    {
        logsCleaner.IsAvailable(OperatingSystemType.MacOS).Returns(false);
        var logsItem = new CleanableItemBuilder()
            .ForModule(CleanupModuleName.System)
            .WithReason("system:user-logs")
            .Large()
            .Build();
        logsCleaner.AnalyzeAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Result<IReadOnlyList<CleanableItem>, DomainError>.Success([logsItem])));

        var result = await module.AnalyzeAsync(CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        await logsCleaner.DidNotReceive().AnalyzeAsync(Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task AggregateCleanupResultsFromAllCleaners()
    {
        var item = new CleanableItemBuilder().ForModule(CleanupModuleName.System).Large().Build();
        cacheCleaner.CleanAsync(Arg.Any<IReadOnlyList<CleanableItem>>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(CleanupResult.Create(1, FileSize.Create(2048).Value)));
        logsCleaner.CleanAsync(Arg.Any<IReadOnlyList<CleanableItem>>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(CleanupResult.Create(1, FileSize.Create(1024).Value)));

        var result = await module.CleanAsync([item], CancellationToken.None);

        var cleanupResult = result.Value;
        result.IsSuccess.Should().BeTrue();
        cleanupResult.TotalFilesDeleted().Should().Be(2);
    }

    [Test]
    public async Task SucceedWithEmptyResultWhenNoItemsProvided()
    {
        var result = await module.CleanAsync([], CancellationToken.None);

        var cleanupResult = result.Value;
        result.IsSuccess.Should().BeTrue();
        cleanupResult.TotalFilesDeleted().Should().Be(0);
    }
}

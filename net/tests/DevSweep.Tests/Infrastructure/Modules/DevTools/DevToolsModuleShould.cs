using AwesomeAssertions;
using DevSweep.Application.Ports.Driven;
using DevSweep.Domain.Common;
using DevSweep.Domain.Entities;
using DevSweep.Domain.Enums;
using DevSweep.Domain.Errors;
using DevSweep.Domain.ValueObjects;
using DevSweep.Infrastructure.Modules.DevTools;
using DevSweep.Tests.Builders;
using NSubstitute;

namespace DevSweep.Tests.Infrastructure.Modules.DevTools;

internal sealed class DevToolsModuleShould
{
    private readonly IDevToolsCleaner mavenCleaner = Substitute.For<IDevToolsCleaner>();
    private readonly IDevToolsCleaner sdkmanCleaner = Substitute.For<IDevToolsCleaner>();
    private readonly IEnvironmentProvider environment = Substitute.For<IEnvironmentProvider>();
    private readonly DevToolsModule module;

    public DevToolsModuleShould()
    {
        environment.CurrentOperatingSystem.Returns(OperatingSystemType.MacOS);

        mavenCleaner.CleanerName.Returns("Maven");
        mavenCleaner.IsAvailable(Arg.Any<OperatingSystemType>()).Returns(true);
        mavenCleaner.AnalyzeAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Result<IReadOnlyList<CleanableItem>, DomainError>.Success([])));
        mavenCleaner.CleanAsync(Arg.Any<IReadOnlyList<CleanableItem>>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Result<CleanupResult, DomainError>.Success(CleanupResult.Empty)));

        sdkmanCleaner.CleanerName.Returns("SDKMAN");
        sdkmanCleaner.IsAvailable(OperatingSystemType.MacOS).Returns(true);
        sdkmanCleaner.IsAvailable(OperatingSystemType.Linux).Returns(true);
        sdkmanCleaner.IsAvailable(OperatingSystemType.Windows).Returns(false);
        sdkmanCleaner.AnalyzeAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Result<IReadOnlyList<CleanableItem>, DomainError>.Success([])));
        sdkmanCleaner.CleanAsync(Arg.Any<IReadOnlyList<CleanableItem>>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Result<CleanupResult, DomainError>.Success(CleanupResult.Empty)));

        module = new DevToolsModule(environment, [mavenCleaner, sdkmanCleaner]);
    }

    [Test]
    public void BeAvailableOnAllPlatforms()
    {
        module.IsAvailableOnPlatform(OperatingSystemType.MacOS).Should().BeTrue();
        module.IsAvailableOnPlatform(OperatingSystemType.Linux).Should().BeTrue();
        module.IsAvailableOnPlatform(OperatingSystemType.Windows).Should().BeTrue();
    }

    [Test]
    public void BeMarkedAsDestructive()
    {
        module.IsDestructive.Should().BeTrue();
    }

    [Test]
    public void HaveDevToolsModuleName()
    {
        module.Name.Should().Be(CleanupModuleName.DevTools);
    }

    [Test]
    public async Task AggregateItemsFromAllActiveCleaners()
    {
        var mavenItem = new CleanableItemBuilder()
            .ForModule(CleanupModuleName.DevTools)
            .WithReason("devtools:maven-dependency-cache")
            .Large()
            .Build();
        var sdkmanItem = new CleanableItemBuilder()
            .ForModule(CleanupModuleName.DevTools)
            .WithReason("devtools:sdkman-tmp")
            .Small()
            .Build();
        mavenCleaner.AnalyzeAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Result<IReadOnlyList<CleanableItem>, DomainError>.Success([mavenItem])));
        sdkmanCleaner.AnalyzeAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Result<IReadOnlyList<CleanableItem>, DomainError>.Success([sdkmanItem])));

        var result = await module.AnalyzeAsync(CancellationToken.None);

        var analysis = result.Value;
        result.IsSuccess.Should().BeTrue();
        analysis.ItemCount().Should().Be(2);
    }

    [Test]
    public async Task SkipCleanersNotAvailableOnCurrentPlatform()
    {
        environment.CurrentOperatingSystem.Returns(OperatingSystemType.Windows);
        var sdkmanItem = new CleanableItemBuilder()
            .ForModule(CleanupModuleName.DevTools)
            .WithReason("devtools:sdkman-tmp")
            .Large()
            .Build();
        sdkmanCleaner.AnalyzeAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Result<IReadOnlyList<CleanableItem>, DomainError>.Success([sdkmanItem])));

        var result = await module.AnalyzeAsync(CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        await sdkmanCleaner.DidNotReceive().AnalyzeAsync(Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task FindNoItemsWhenAllCleanersReturnEmpty()
    {
        var result = await module.AnalyzeAsync(CancellationToken.None);

        var analysis = result.Value;
        result.IsSuccess.Should().BeTrue();
        analysis.ItemCount().Should().Be(0);
    }

    [Test]
    public async Task DelegateCleanToAllActiveCleaners()
    {
        var item = new CleanableItemBuilder().ForModule(CleanupModuleName.DevTools).Build();

        await module.CleanAsync([item], CancellationToken.None);

        await mavenCleaner.Received(1).CleanAsync(Arg.Any<IReadOnlyList<CleanableItem>>(), Arg.Any<CancellationToken>());
        await sdkmanCleaner.Received(1).CleanAsync(Arg.Any<IReadOnlyList<CleanableItem>>(), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task CleanNothingWhenItemListIsEmpty()
    {
        var result = await module.CleanAsync([], CancellationToken.None);

        var cleanupResult = result.Value;
        result.IsSuccess.Should().BeTrue();
        cleanupResult.TotalFilesDeleted().Should().Be(0);
    }

    [Test]
    public async Task AggregateCleanupResultsFromAllCleaners()
    {
        var item = new CleanableItemBuilder().ForModule(CleanupModuleName.DevTools).Large().Build();
        mavenCleaner.CleanAsync(Arg.Any<IReadOnlyList<CleanableItem>>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(CleanupResult.Create(1, FileSize.Create(2048).Value)));
        sdkmanCleaner.CleanAsync(Arg.Any<IReadOnlyList<CleanableItem>>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(CleanupResult.Create(1, FileSize.Create(1024).Value)));

        var result = await module.CleanAsync([item], CancellationToken.None);

        var cleanupResult = result.Value;
        result.IsSuccess.Should().BeTrue();
        cleanupResult.TotalFilesDeleted().Should().Be(2);
    }
}

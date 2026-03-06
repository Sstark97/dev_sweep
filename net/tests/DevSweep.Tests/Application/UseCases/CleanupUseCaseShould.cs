using DevSweep.Application.Models;
using DevSweep.Application.Modules;
using DevSweep.Application.Ports.Driven;
using DevSweep.Application.UseCases;
using DevSweep.Domain.Entities;
using DevSweep.Domain.Enums;
using DevSweep.Domain.Errors;
using DevSweep.Tests.Application.Builders;
using DevSweep.Tests.Builders;

namespace DevSweep.Tests.Application.UseCases;

public class CleanupUseCaseShould
{
    private readonly ModuleRegistry registry = new();
    private readonly IOutputFormatter outputFormatter = Substitute.For<IOutputFormatter>();
    private readonly IUserInteraction userInteraction = Substitute.For<IUserInteraction>();

    [Fact]
    public async Task ReturnEmptyListWhenNoModulesRequested()
    {
        var useCase = new CleanupUseCase(registry, outputFormatter, userInteraction);

        var result = await useCase.Invoke([], CancellationToken.None);

        var summaries = result.Value;

        result.IsSuccess.Should().BeTrue();
        summaries.Should().BeEmpty();
    }

    [Fact]
    public async Task CleanSingleModuleSuccessfully()
    {
        var dockerItem = new CleanableItemBuilder().ForModule(CleanupModuleName.Docker).Build();
        var dockerAnalysis = ModuleAnalysis.Create(CleanupModuleName.Docker, [dockerItem]).Value;
        var dockerCleanupResult = new CleanupResultBuilder().Build();

        var dockerModule = Substitute.For<ICleanupModule>();
        new CleanupModuleBuilder(dockerModule)
            .ForModule(CleanupModuleName.Docker)
            .WithAnalysis(dockerAnalysis)
            .WithCleanResult(dockerCleanupResult)
            .Configure();

        registry.Register(dockerModule);
        var useCase = new CleanupUseCase(registry, outputFormatter, userInteraction);

        var result = await useCase.Invoke([CleanupModuleName.Docker], CancellationToken.None);

        var summaries = result.Value;

        result.IsSuccess.Should().BeTrue();
        summaries.Should().HaveCount(1);
        summaries[0].Module.Should().Be(CleanupModuleName.Docker);
    }

    [Fact]
    public async Task CleanMultipleModulesSuccessfully()
    {
        var dockerItem = new CleanableItemBuilder().ForModule(CleanupModuleName.Docker).Build();
        var homebrewItem = new CleanableItemBuilder().ForModule(CleanupModuleName.Homebrew).Build();
        var dockerAnalysis = ModuleAnalysis.Create(CleanupModuleName.Docker, [dockerItem]).Value;
        var homebrewAnalysis = ModuleAnalysis.Create(CleanupModuleName.Homebrew, [homebrewItem]).Value;
        var cleanupResult = new CleanupResultBuilder().Build();

        var dockerModule = Substitute.For<ICleanupModule>();
        new CleanupModuleBuilder(dockerModule)
            .ForModule(CleanupModuleName.Docker)
            .WithAnalysis(dockerAnalysis)
            .WithCleanResult(cleanupResult)
            .Configure();

        var homebrewModule = Substitute.For<ICleanupModule>();
        new CleanupModuleBuilder(homebrewModule)
            .ForModule(CleanupModuleName.Homebrew)
            .WithAnalysis(homebrewAnalysis)
            .WithCleanResult(cleanupResult)
            .Configure();

        registry.Register(dockerModule);
        registry.Register(homebrewModule);
        var useCase = new CleanupUseCase(registry, outputFormatter, userInteraction);

        var result = await useCase.Invoke(
            [CleanupModuleName.Docker, CleanupModuleName.Homebrew],
            CancellationToken.None);

        var summaries = result.Value;

        result.IsSuccess.Should().BeTrue();
        summaries.Should().HaveCount(2);
    }

    [Fact]
    public async Task SkipModuleWhenAnalysisIsEmpty()
    {
        var emptyAnalysis = ModuleAnalysis.CreateEmpty(CleanupModuleName.Docker);

        var dockerModule = Substitute.For<ICleanupModule>();
        new CleanupModuleBuilder(dockerModule)
            .ForModule(CleanupModuleName.Docker)
            .WithAnalysis(emptyAnalysis)
            .Configure();

        registry.Register(dockerModule);
        var useCase = new CleanupUseCase(registry, outputFormatter, userInteraction);

        var result = await useCase.Invoke([CleanupModuleName.Docker], CancellationToken.None);

        var summaries = result.Value;

        result.IsSuccess.Should().BeTrue();
        summaries.Should().BeEmpty();
        await dockerModule.DidNotReceive().CleanAsync(
            Arg.Any<IReadOnlyList<CleanableItem>>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ConfirmOnceWhenAnyModuleIsDestructive()
    {
        var dockerItem = new CleanableItemBuilder().ForModule(CleanupModuleName.Docker).Build();
        var homebrewItem = new CleanableItemBuilder().ForModule(CleanupModuleName.Homebrew).Build();
        var dockerAnalysis = ModuleAnalysis.Create(CleanupModuleName.Docker, [dockerItem]).Value;
        var homebrewAnalysis = ModuleAnalysis.Create(CleanupModuleName.Homebrew, [homebrewItem]).Value;
        var cleanupResult = new CleanupResultBuilder().Build();

        var destructiveModule = Substitute.For<ICleanupModule>();
        new CleanupModuleBuilder(destructiveModule)
            .ForModule(CleanupModuleName.Docker)
            .Destructive()
            .WithAnalysis(dockerAnalysis)
            .WithCleanResult(cleanupResult)
            .Configure();

        var safeModule = Substitute.For<ICleanupModule>();
        new CleanupModuleBuilder(safeModule)
            .ForModule(CleanupModuleName.Homebrew)
            .WithAnalysis(homebrewAnalysis)
            .WithCleanResult(cleanupResult)
            .Configure();

        userInteraction.ConfirmAsync(Arg.Any<string>(), Arg.Any<bool>(), Arg.Any<CancellationToken>())
            .Returns(true);

        registry.Register(destructiveModule);
        registry.Register(safeModule);
        var useCase = new CleanupUseCase(registry, outputFormatter, userInteraction);

        var result = await useCase.Invoke(
            [CleanupModuleName.Docker, CleanupModuleName.Homebrew],
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        await userInteraction.Received(1).ConfirmAsync(
            Arg.Any<string>(), isDestructive: true, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ReturnEmptyListWhenUserDeclinesConfirmation()
    {
        var dockerItem = new CleanableItemBuilder().ForModule(CleanupModuleName.Docker).Build();
        var dockerAnalysis = ModuleAnalysis.Create(CleanupModuleName.Docker, [dockerItem]).Value;

        var destructiveModule = Substitute.For<ICleanupModule>();
        new CleanupModuleBuilder(destructiveModule)
            .ForModule(CleanupModuleName.Docker)
            .Destructive()
            .WithAnalysis(dockerAnalysis)
            .Configure();

        userInteraction.ConfirmAsync(Arg.Any<string>(), Arg.Any<bool>(), Arg.Any<CancellationToken>())
            .Returns(false);

        registry.Register(destructiveModule);
        var useCase = new CleanupUseCase(registry, outputFormatter, userInteraction);

        var result = await useCase.Invoke([CleanupModuleName.Docker], CancellationToken.None);

        var summaries = result.Value;

        result.IsSuccess.Should().BeTrue();
        summaries.Should().BeEmpty();
        await destructiveModule.DidNotReceive().CleanAsync(
            Arg.Any<IReadOnlyList<CleanableItem>>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SkipConfirmationWhenNoDestructiveModules()
    {
        var dockerItem = new CleanableItemBuilder().ForModule(CleanupModuleName.Docker).Build();
        var dockerAnalysis = ModuleAnalysis.Create(CleanupModuleName.Docker, [dockerItem]).Value;
        var cleanupResult = new CleanupResultBuilder().Build();

        var safeModule = Substitute.For<ICleanupModule>();
        new CleanupModuleBuilder(safeModule)
            .ForModule(CleanupModuleName.Docker)
            .WithAnalysis(dockerAnalysis)
            .WithCleanResult(cleanupResult)
            .Configure();

        registry.Register(safeModule);
        var useCase = new CleanupUseCase(registry, outputFormatter, userInteraction);

        var result = await useCase.Invoke([CleanupModuleName.Docker], CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        await userInteraction.DidNotReceive().ConfirmAsync(
            Arg.Any<string>(), Arg.Any<bool>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task DisplayCompletionViaOutputFormatter()
    {
        var dockerItem = new CleanableItemBuilder().ForModule(CleanupModuleName.Docker).Build();
        var dockerAnalysis = ModuleAnalysis.Create(CleanupModuleName.Docker, [dockerItem]).Value;
        var cleanupResult = new CleanupResultBuilder().Build();

        var dockerModule = Substitute.For<ICleanupModule>();
        new CleanupModuleBuilder(dockerModule)
            .ForModule(CleanupModuleName.Docker)
            .WithAnalysis(dockerAnalysis)
            .WithCleanResult(cleanupResult)
            .Configure();

        registry.Register(dockerModule);
        var useCase = new CleanupUseCase(registry, outputFormatter, userInteraction);

        var result = await useCase.Invoke([CleanupModuleName.Docker], CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        outputFormatter.Received(1).DisplayCompletion(Arg.Any<IReadOnlyList<CleanupSummary>>());
    }

    [Fact]
    public async Task FailWhenModuleNotFoundInRegistry()
    {
        var useCase = new CleanupUseCase(registry, outputFormatter, userInteraction);

        var result = await useCase.Invoke([CleanupModuleName.Docker], CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.IsNotFoundError().Should().BeTrue();
    }

    [Fact]
    public async Task FailWhenModuleAnalysisReturnsError()
    {
        var analysisError = DomainError.InvalidOperation("Analysis failed");

        var dockerModule = Substitute.For<ICleanupModule>();
        new CleanupModuleBuilder(dockerModule)
            .ForModule(CleanupModuleName.Docker)
            .WithAnalysisError(analysisError)
            .Configure();

        registry.Register(dockerModule);
        var useCase = new CleanupUseCase(registry, outputFormatter, userInteraction);

        var result = await useCase.Invoke([CleanupModuleName.Docker], CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.MessageContains("Analysis failed").Should().BeTrue();
    }

    [Fact]
    public async Task FailWhenModuleCleanReturnsError()
    {
        var dockerItem = new CleanableItemBuilder().ForModule(CleanupModuleName.Docker).Build();
        var dockerAnalysis = ModuleAnalysis.Create(CleanupModuleName.Docker, [dockerItem]).Value;
        var cleanError = DomainError.InvalidOperation("Clean failed");

        var dockerModule = Substitute.For<ICleanupModule>();
        new CleanupModuleBuilder(dockerModule)
            .ForModule(CleanupModuleName.Docker)
            .WithAnalysis(dockerAnalysis)
            .WithCleanError(cleanError)
            .Configure();

        registry.Register(dockerModule);
        var useCase = new CleanupUseCase(registry, outputFormatter, userInteraction);

        var result = await useCase.Invoke([CleanupModuleName.Docker], CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.MessageContains("Clean failed").Should().BeTrue();
    }

    [Fact]
    public async Task FailWhenModulesListIsNull()
    {
        var useCase = new CleanupUseCase(registry, outputFormatter, userInteraction);

        var result = await useCase.Invoke(null!, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.IsValidationError().Should().BeTrue();
    }
}

using DevSweep.Application.Models;
using DevSweep.Application.Modules;
using DevSweep.Application.Ports.Driven;
using DevSweep.Application.UseCases;
using DevSweep.Domain.Enums;
using DevSweep.Domain.Errors;
using DevSweep.Tests.Application.Builders;
using DevSweep.Tests.Builders;

namespace DevSweep.Tests.Application.UseCases;

public class AnalyzeUseCaseShould
{
    private readonly ModuleRegistry registry = new();
    private readonly IOutputFormatter outputFormatter = Substitute.For<IOutputFormatter>();

    [Fact]
    public async Task ReturnEmptyReportWhenNoModulesRequested()
    {
        var useCase = new AnalyzeUseCase(registry, outputFormatter);

        var result = await useCase.Invoke([], CancellationToken.None);

        var report = result.Value;

        result.IsSuccess.Should().BeTrue();
        report.IsEmpty().Should().BeTrue();
    }

    [Fact]
    public async Task AnalyzeSingleModuleSuccessfully()
    {
        var dockerItem = new CleanableItemBuilder().ForModule(CleanupModuleName.Docker).Build();
        var dockerAnalysis = ModuleAnalysis.Create(CleanupModuleName.Docker, [dockerItem]).Value;

        var dockerModule = Substitute.For<ICleanupModule>();
        new CleanupModuleBuilder(dockerModule)
            .ForModule(CleanupModuleName.Docker)
            .WithAnalysis(dockerAnalysis)
            .Configure();

        registry.Register(dockerModule);
        var useCase = new AnalyzeUseCase(registry, outputFormatter);

        var result = await useCase.Invoke([CleanupModuleName.Docker], CancellationToken.None);

        var report = result.Value;

        result.IsSuccess.Should().BeTrue();
        report.ModuleCount().Should().Be(1);
        report.TotalItemCount().Should().Be(1);
    }

    [Fact]
    public async Task AnalyzeMultipleModulesSuccessfully()
    {
        var dockerItem = new CleanableItemBuilder().ForModule(CleanupModuleName.Docker).Build();
        var homebrewItem = new CleanableItemBuilder().ForModule(CleanupModuleName.Homebrew).Build();
        var dockerAnalysis = ModuleAnalysis.Create(CleanupModuleName.Docker, [dockerItem]).Value;
        var homebrewAnalysis = ModuleAnalysis.Create(CleanupModuleName.Homebrew, [homebrewItem]).Value;

        var dockerModule = Substitute.For<ICleanupModule>();
        new CleanupModuleBuilder(dockerModule)
            .ForModule(CleanupModuleName.Docker)
            .WithAnalysis(dockerAnalysis)
            .Configure();

        var homebrewModule = Substitute.For<ICleanupModule>();
        new CleanupModuleBuilder(homebrewModule)
            .ForModule(CleanupModuleName.Homebrew)
            .WithAnalysis(homebrewAnalysis)
            .Configure();

        registry.Register(dockerModule);
        registry.Register(homebrewModule);
        var useCase = new AnalyzeUseCase(registry, outputFormatter);

        var result = await useCase.Invoke(
            [CleanupModuleName.Docker, CleanupModuleName.Homebrew],
            CancellationToken.None);

        var report = result.Value;

        result.IsSuccess.Should().BeTrue();
        report.ModuleCount().Should().Be(2);
        report.TotalItemCount().Should().Be(2);
    }

    [Fact]
    public async Task DisplayAnalysisReportViaOutputFormatter()
    {
        var dockerItem = new CleanableItemBuilder().ForModule(CleanupModuleName.Docker).Build();
        var dockerAnalysis = ModuleAnalysis.Create(CleanupModuleName.Docker, [dockerItem]).Value;

        var dockerModule = Substitute.For<ICleanupModule>();
        new CleanupModuleBuilder(dockerModule)
            .ForModule(CleanupModuleName.Docker)
            .WithAnalysis(dockerAnalysis)
            .Configure();

        registry.Register(dockerModule);
        var useCase = new AnalyzeUseCase(registry, outputFormatter);

        var result = await useCase.Invoke([CleanupModuleName.Docker], CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        outputFormatter.Received(1).DisplayAnalysisReport(Arg.Any<AnalysisReport>());
    }

    [Fact]
    public async Task FailWhenModuleNotFoundInRegistry()
    {
        var useCase = new AnalyzeUseCase(registry, outputFormatter);

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
        var useCase = new AnalyzeUseCase(registry, outputFormatter);

        var result = await useCase.Invoke([CleanupModuleName.Docker], CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.MessageContains("Analysis failed").Should().BeTrue();
    }

    [Fact]
    public async Task FailWhenModulesListIsNull()
    {
        var useCase = new AnalyzeUseCase(registry, outputFormatter);

        var result = await useCase.Invoke(null!, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.IsValidationError().Should().BeTrue();
    }
}

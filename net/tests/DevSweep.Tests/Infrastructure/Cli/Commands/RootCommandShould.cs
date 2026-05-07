using AwesomeAssertions;
using DevSweep.Application.Models;
using DevSweep.Application.Ports.Driven;
using DevSweep.Application.Ports.Driving;
using DevSweep.Domain.Common;
using DevSweep.Domain.Entities;
using DevSweep.Domain.Enums;
using DevSweep.Domain.Errors;
using DevSweep.Infrastructure.Cli.Commands;
using NSubstitute;

namespace DevSweep.Tests.Infrastructure.Cli.Commands;

internal sealed class RootCommandShould
{
    private readonly IOutputFormatter outputFormatter = Substitute.For<IOutputFormatter>();
    private readonly IAvailableModulesUseCase availableModulesUseCase = Substitute.For<IAvailableModulesUseCase>();
    private readonly IModuleSelector moduleSelector = Substitute.For<IModuleSelector>();
    private readonly IAnalyzeUseCase analyzeUseCase = Substitute.For<IAnalyzeUseCase>();
    private readonly ICleanupUseCase cleanupUseCase = Substitute.For<ICleanupUseCase>();

    [Test]
    public async Task DisplayBannerWhenInvokedWithoutSubcommand()
    {
        GivenNoAvailableModules();
        var command = BuildCommand();

        await command.RunAsync();

        outputFormatter.Received().DisplayBanner(Arg.Any<string>());
    }

    [Test]
    public async Task ExitGracefullyWhenNoModulesAvailable()
    {
        GivenNoAvailableModules();
        var command = BuildCommand();

        var exitCode = await command.RunAsync();

        exitCode.Should().Be(0);
        await moduleSelector.DidNotReceive().SelectModulesAsync(
            Arg.Any<IReadOnlyList<ModuleDescriptor>>(),
            Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task PresentModuleSelectionWhenModulesAvailable()
    {
        var dockerDescriptor = ModuleDescriptor.Create(CleanupModuleName.Docker, "Docker cache", false).Value;
        GivenAvailableModules([dockerDescriptor]);
        GivenUserSelectsNoModules();
        var command = BuildCommand();

        await command.RunAsync();

        await moduleSelector.Received().SelectModulesAsync(
            Arg.Is<IReadOnlyList<ModuleDescriptor>>(m => m.Count == 1),
            Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task ExitGracefullyWhenUserSelectsNoModules()
    {
        var dockerDescriptor = ModuleDescriptor.Create(CleanupModuleName.Docker, "Docker cache", false).Value;
        GivenAvailableModules([dockerDescriptor]);
        GivenUserSelectsNoModules();
        var command = BuildCommand();

        var exitCode = await command.RunAsync();

        exitCode.Should().Be(0);
        await analyzeUseCase.DidNotReceive().Invoke(
            Arg.Any<IReadOnlyList<CleanupModuleName>>(),
            Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task RunAnalysisForSelectedModules()
    {
        var dockerDescriptor = ModuleDescriptor.Create(CleanupModuleName.Docker, "Docker cache", false).Value;
        GivenAvailableModules([dockerDescriptor]);
        GivenUserSelectsModules([CleanupModuleName.Docker]);
        GivenAnalysisSucceeds();
        GivenCleanupSucceeds();
        var command = BuildCommand();

        await command.RunAsync();

        await analyzeUseCase.Received().Invoke(
            Arg.Is<IReadOnlyList<CleanupModuleName>>(m => m.Contains(CleanupModuleName.Docker)),
            Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task RunCleanupAfterAnalysis()
    {
        var dockerDescriptor = ModuleDescriptor.Create(CleanupModuleName.Docker, "Docker cache", false).Value;
        GivenAvailableModules([dockerDescriptor]);
        GivenUserSelectsModules([CleanupModuleName.Docker]);
        GivenAnalysisSucceeds();
        GivenCleanupSucceeds();
        var command = BuildCommand();

        await command.RunAsync();

        await cleanupUseCase.Received().Invoke(
            Arg.Is<IReadOnlyList<CleanupModuleName>>(m => m.Contains(CleanupModuleName.Docker)),
            Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task SkipCleanupWhenDryRunIsEnabled()
    {
        var dockerDescriptor = ModuleDescriptor.Create(CleanupModuleName.Docker, "Docker cache", false).Value;
        GivenAvailableModules([dockerDescriptor]);
        GivenUserSelectsModules([CleanupModuleName.Docker]);
        GivenAnalysisSucceeds();
        var command = BuildCommand();
        command.DryRun = true;

        var exitCode = await command.RunAsync();

        exitCode.Should().Be(0);
        await cleanupUseCase.DidNotReceive().Invoke(
            Arg.Any<IReadOnlyList<CleanupModuleName>>(),
            Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task RunAnalysisWhenDryRunIsEnabled()
    {
        var dockerDescriptor = ModuleDescriptor.Create(CleanupModuleName.Docker, "Docker cache", false).Value;
        GivenAvailableModules([dockerDescriptor]);
        GivenUserSelectsModules([CleanupModuleName.Docker]);
        GivenAnalysisSucceeds();
        var command = BuildCommand();
        command.DryRun = true;

        await command.RunAsync();

        await analyzeUseCase.Received().Invoke(
            Arg.Is<IReadOnlyList<CleanupModuleName>>(m => m.Contains(CleanupModuleName.Docker)),
            Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task FailWhenAnalysisEncountersError()
    {
        var dockerDescriptor = ModuleDescriptor.Create(CleanupModuleName.Docker, "Docker cache", false).Value;
        GivenAvailableModules([dockerDescriptor]);
        GivenUserSelectsModules([CleanupModuleName.Docker]);
        GivenAnalysisFails("Analysis error");
        var command = BuildCommand();

        var exitCode = await command.RunAsync();

        exitCode.Should().Be(1);
        outputFormatter.Received().Error(Arg.Any<string>());
    }

    [Test]
    public async Task FailWhenCleanupEncountersError()
    {
        var dockerDescriptor = ModuleDescriptor.Create(CleanupModuleName.Docker, "Docker cache", false).Value;
        GivenAvailableModules([dockerDescriptor]);
        GivenUserSelectsModules([CleanupModuleName.Docker]);
        GivenAnalysisSucceeds();
        GivenCleanupFails("Cleanup error");
        var command = BuildCommand();

        var exitCode = await command.RunAsync();

        exitCode.Should().Be(1);
        outputFormatter.Received().Error(Arg.Any<string>());
    }

    [Test]
    public async Task SucceedWhenFullFlowCompletes()
    {
        var dockerDescriptor = ModuleDescriptor.Create(CleanupModuleName.Docker, "Docker cache", false).Value;
        GivenAvailableModules([dockerDescriptor]);
        GivenUserSelectsModules([CleanupModuleName.Docker]);
        GivenAnalysisSucceeds();
        GivenCleanupSucceeds();
        var command = BuildCommand();

        var exitCode = await command.RunAsync();

        exitCode.Should().Be(0);
    }

    private RootCommand BuildCommand() =>
        new(outputFormatter, availableModulesUseCase, moduleSelector, analyzeUseCase, cleanupUseCase);

    private void GivenNoAvailableModules() =>
        availableModulesUseCase.Invoke().Returns([]);

    private void GivenAvailableModules(IReadOnlyList<ModuleDescriptor> modules) =>
        availableModulesUseCase.Invoke().Returns(modules);

    private void GivenUserSelectsModules(IReadOnlyList<CleanupModuleName> modules) =>
        moduleSelector.SelectModulesAsync(
            Arg.Any<IReadOnlyList<ModuleDescriptor>>(),
            Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(modules));

    private void GivenUserSelectsNoModules() =>
        moduleSelector.SelectModulesAsync(
            Arg.Any<IReadOnlyList<ModuleDescriptor>>(),
            Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IReadOnlyList<CleanupModuleName>>([]));

    private void GivenAnalysisSucceeds()
    {
        var emptyReport = AnalysisReport.Create([]).Value;
        analyzeUseCase.Invoke(Arg.Any<IReadOnlyList<CleanupModuleName>>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Result<AnalysisReport, DomainError>.Success(emptyReport)));
    }

    private void GivenAnalysisFails(string errorMessage) =>
        analyzeUseCase.Invoke(Arg.Any<IReadOnlyList<CleanupModuleName>>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(
                Result<AnalysisReport, DomainError>.Failure(DomainError.InvalidOperation(errorMessage))));

    private void GivenCleanupSucceeds() =>
        cleanupUseCase.Invoke(Arg.Any<IReadOnlyList<CleanupModuleName>>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(
                Result<IReadOnlyList<CleanupSummary>, DomainError>.Success([])));

    private void GivenCleanupFails(string errorMessage) =>
        cleanupUseCase.Invoke(Arg.Any<IReadOnlyList<CleanupModuleName>>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(
                Result<IReadOnlyList<CleanupSummary>, DomainError>.Failure(DomainError.InvalidOperation(errorMessage))));
}

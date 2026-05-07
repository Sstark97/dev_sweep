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

internal sealed class CleanCommandShould
{
    private readonly ICleanupUseCase cleanupUseCase = Substitute.For<ICleanupUseCase>();
    private readonly IAnalyzeUseCase analyzeUseCase = Substitute.For<IAnalyzeUseCase>();
    private readonly IAvailableModulesUseCase availableModulesUseCase = Substitute.For<IAvailableModulesUseCase>();
    private readonly IOutputFormatter outputFormatter = Substitute.For<IOutputFormatter>();

    [Test]
    public async Task CleanSpecifiedModulesSuccessfully()
    {
        GivenCleanupSucceeds();
        var command = new CleanCommand(cleanupUseCase, analyzeUseCase, availableModulesUseCase, outputFormatter)
        {
            Modules = ["docker"]
        };

        var exitCode = await command.RunAsync();

        exitCode.Should().Be(0);
        await cleanupUseCase.Received().Invoke(
            Arg.Is<IReadOnlyList<CleanupModuleName>>(m => m.Contains(CleanupModuleName.Docker)),
            Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task CleanAllModulesWhenAllFlagSet()
    {
        GivenAvailableModulesReturns([CleanupModuleName.Docker, CleanupModuleName.Homebrew]);
        GivenCleanupSucceeds();
        var command = new CleanCommand(cleanupUseCase, analyzeUseCase, availableModulesUseCase, outputFormatter)
        {
            All = true
        };

        var exitCode = await command.RunAsync();

        exitCode.Should().Be(0);
        availableModulesUseCase.Received().Invoke();
        await cleanupUseCase.Received().Invoke(
            Arg.Is<IReadOnlyList<CleanupModuleName>>(m => m.Count == 2),
            Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task FailWhenNuclearWithoutDevToolsModule()
    {
        var command = new CleanCommand(cleanupUseCase, analyzeUseCase, availableModulesUseCase, outputFormatter)
        {
            Modules = ["docker"],
            Nuclear = true
        };

        var exitCode = await command.RunAsync();

        exitCode.Should().Be(1);
        outputFormatter.Received().Error(Arg.Is<string>(m => m.Contains("devtools")));
    }

    [Test]
    public async Task AllowNuclearWithDevToolsModule()
    {
        GivenCleanupSucceeds();
        var command = new CleanCommand(cleanupUseCase, analyzeUseCase, availableModulesUseCase, outputFormatter)
        {
            Modules = ["devtools"],
            Nuclear = true
        };

        var exitCode = await command.RunAsync();

        exitCode.Should().Be(0);
        await cleanupUseCase.Received().Invoke(
            Arg.Is<IReadOnlyList<CleanupModuleName>>(m => m.Contains(CleanupModuleName.DevTools)),
            Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task FailWhenNoModulesSpecifiedAndAllNotSet()
    {
        var command = new CleanCommand(cleanupUseCase, analyzeUseCase, availableModulesUseCase, outputFormatter)
        {
            Modules = [],
            All = false
        };

        var exitCode = await command.RunAsync();

        exitCode.Should().Be(1);
        outputFormatter.Received().Error(Arg.Any<string>());
    }

    [Test]
    public async Task FailWhenCleanupReturnsError()
    {
        GivenCleanupFails(DomainError.InvalidOperation("Cleanup failed"));
        var command = new CleanCommand(cleanupUseCase, analyzeUseCase, availableModulesUseCase, outputFormatter)
        {
            Modules = ["docker"]
        };

        var exitCode = await command.RunAsync();

        exitCode.Should().Be(1);
        outputFormatter.Received().Error(Arg.Any<string>());
    }

    [Test]
    public async Task RunAnalysisInsteadOfCleanupWhenDryRunIsEnabled()
    {
        GivenAnalysisSucceeds();
        var command = new CleanCommand(cleanupUseCase, analyzeUseCase, availableModulesUseCase, outputFormatter)
        {
            Modules = ["docker"],
            DryRun = true
        };

        var exitCode = await command.RunAsync();

        exitCode.Should().Be(0);
        await analyzeUseCase.Received().Invoke(
            Arg.Is<IReadOnlyList<CleanupModuleName>>(m => m.Contains(CleanupModuleName.Docker)),
            Arg.Any<CancellationToken>());
        await cleanupUseCase.DidNotReceive().Invoke(
            Arg.Any<IReadOnlyList<CleanupModuleName>>(),
            Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task FailWhenAnalysisFailsInDryRunMode()
    {
        GivenAnalysisFails(DomainError.InvalidOperation("Boom"));
        var command = new CleanCommand(cleanupUseCase, analyzeUseCase, availableModulesUseCase, outputFormatter)
        {
            Modules = ["docker"],
            DryRun = true
        };

        var exitCode = await command.RunAsync();

        exitCode.Should().Be(1);
        outputFormatter.Received().Error(Arg.Any<string>());
    }

    [Test]
    public async Task NotInvokeAnalysisWhenDryRunIsNotSet()
    {
        GivenCleanupSucceeds();
        var command = new CleanCommand(cleanupUseCase, analyzeUseCase, availableModulesUseCase, outputFormatter)
        {
            Modules = ["docker"]
        };

        await command.RunAsync();

        await analyzeUseCase.DidNotReceive().Invoke(
            Arg.Any<IReadOnlyList<CleanupModuleName>>(),
            Arg.Any<CancellationToken>());
        await cleanupUseCase.Received().Invoke(
            Arg.Is<IReadOnlyList<CleanupModuleName>>(m => m.Contains(CleanupModuleName.Docker)),
            Arg.Any<CancellationToken>());
    }

    private void GivenCleanupSucceeds()
    {
        cleanupUseCase.Invoke(Arg.Any<IReadOnlyList<CleanupModuleName>>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(
                Result<IReadOnlyList<CleanupSummary>, DomainError>.Success([])));
    }

    private void GivenCleanupFails(DomainError error)
    {
        cleanupUseCase.Invoke(Arg.Any<IReadOnlyList<CleanupModuleName>>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(
                Result<IReadOnlyList<CleanupSummary>, DomainError>.Failure(error)));
    }

    private void GivenAnalysisSucceeds()
    {
        var emptyReport = AnalysisReport.Create([]).Value;
        analyzeUseCase.Invoke(Arg.Any<IReadOnlyList<CleanupModuleName>>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Result<AnalysisReport, DomainError>.Success(emptyReport)));
    }

    private void GivenAnalysisFails(DomainError error)
    {
        analyzeUseCase.Invoke(Arg.Any<IReadOnlyList<CleanupModuleName>>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Result<AnalysisReport, DomainError>.Failure(error)));
    }

    private void GivenAvailableModulesReturns(IReadOnlyList<CleanupModuleName> modules)
    {
        var descriptors = modules
            .Select(m => ModuleDescriptor.Create(m, "description", false).Value)
            .ToList();
        availableModulesUseCase.Invoke().Returns(descriptors);
    }
}

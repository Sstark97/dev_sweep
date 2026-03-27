using AwesomeAssertions;
using DevSweep.Application.Models;
using DevSweep.Application.Ports.Driven;
using DevSweep.Application.Ports.Driving;
using DevSweep.Domain.Common;
using DevSweep.Domain.Enums;
using DevSweep.Domain.Errors;
using DevSweep.Infrastructure.Cli.Commands;
using NSubstitute;

namespace DevSweep.Tests.Infrastructure.Cli.Commands;

internal sealed class AnalyzeCommandShould
{
    private readonly IAnalyzeUseCase analyzeUseCase = Substitute.For<IAnalyzeUseCase>();
    private readonly IAvailableModulesUseCase availableModulesUseCase = Substitute.For<IAvailableModulesUseCase>();
    private readonly IOutputFormatter outputFormatter = Substitute.For<IOutputFormatter>();

    [Test]
    public async Task AnalyzeSpecifiedModulesSuccessfully()
    {
        GivenAnalyzeSucceeds();
        var command = new AnalyzeCommand(analyzeUseCase, availableModulesUseCase, outputFormatter)
        {
            Modules = ["docker"]
        };

        var exitCode = await command.RunAsync();

        exitCode.Should().Be(0);
        await analyzeUseCase.Received().Invoke(
            Arg.Is<IReadOnlyList<CleanupModuleName>>(m => m.Contains(CleanupModuleName.Docker)),
            Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task AnalyzeAllModulesWhenAllFlagSet()
    {
        GivenAvailableModulesReturns([CleanupModuleName.Docker, CleanupModuleName.Homebrew]);
        GivenAnalyzeSucceeds();
        var command = new AnalyzeCommand(analyzeUseCase, availableModulesUseCase, outputFormatter)
        {
            All = true
        };

        var exitCode = await command.RunAsync();

        exitCode.Should().Be(0);
        availableModulesUseCase.Received().Invoke();
        await analyzeUseCase.Received().Invoke(
            Arg.Is<IReadOnlyList<CleanupModuleName>>(m => m.Count == 2),
            Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task FailWhenNoModulesSpecifiedAndAllNotSet()
    {
        var command = new AnalyzeCommand(analyzeUseCase, availableModulesUseCase, outputFormatter)
        {
            Modules = [],
            All = false
        };

        var exitCode = await command.RunAsync();

        exitCode.Should().Be(1);
        outputFormatter.Received().Error(Arg.Any<string>());
    }

    [Test]
    public async Task FailWhenInvalidModuleNameProvided()
    {
        var command = new AnalyzeCommand(analyzeUseCase, availableModulesUseCase, outputFormatter)
        {
            Modules = ["invalid"]
        };

        var exitCode = await command.RunAsync();

        exitCode.Should().Be(1);
        outputFormatter.Received().Error(Arg.Any<string>());
    }

    [Test]
    public async Task FailWhenAnalysisReturnsError()
    {
        GivenAnalyzeFails(DomainError.InvalidOperation("Analysis failed"));
        var command = new AnalyzeCommand(analyzeUseCase, availableModulesUseCase, outputFormatter)
        {
            Modules = ["docker"]
        };

        var exitCode = await command.RunAsync();

        exitCode.Should().Be(1);
        outputFormatter.Received().Error(Arg.Any<string>());
    }

    private void GivenAnalyzeSucceeds()
    {
        var emptyReport = AnalysisReport.Create([]).Value;
        analyzeUseCase.Invoke(Arg.Any<IReadOnlyList<CleanupModuleName>>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Result<AnalysisReport, DomainError>.Success(emptyReport)));
    }

    private void GivenAnalyzeFails(DomainError error)
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

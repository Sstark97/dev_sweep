using AwesomeAssertions;
using DevSweep.Application.Models;
using DevSweep.Domain.Entities;
using DevSweep.Domain.Enums;
using DevSweep.Infrastructure.Cli.Output;
using DevSweep.Tests.Builders;
using Spectre.Console;

namespace DevSweep.Tests.Infrastructure.Cli.Output;

internal sealed class SpectreOutputFormatterShould
{
    private static (SpectreOutputFormatter Formatter, StringWriter Writer) GivenFormatterWithCapture()
    {
        var writer = new StringWriter();
        var console = AnsiConsole.Create(new AnsiConsoleSettings
        {
            Out = new AnsiConsoleOutput(writer),
            ColorSystem = ColorSystemSupport.NoColors,
            Ansi = AnsiSupport.No
        });
        return (new SpectreOutputFormatter(console), writer);
    }

    [Test]
    public void WriteInfoMessage()
    {
        var (formatter, writer) = GivenFormatterWithCapture();

        formatter.Info("Hello world");

        var text = writer.ToString();
        text.Should().Contain("Hello world");
    }

    [Test]
    public void WriteErrorMessage()
    {
        var (formatter, writer) = GivenFormatterWithCapture();

        formatter.Error("Something failed");

        var text = writer.ToString();
        text.Should().Contain("Something failed");
    }

    [Test]
    public void WriteWarningMessage()
    {
        var (formatter, writer) = GivenFormatterWithCapture();

        formatter.Warning("Watch out");

        var text = writer.ToString();
        text.Should().Contain("Watch out");
    }

    [Test]
    public void WriteSuccessMessage()
    {
        var (formatter, writer) = GivenFormatterWithCapture();

        formatter.Success("Done");

        var text = writer.ToString();
        text.Should().Contain("Done");
    }

    [Test]
    public void WriteBannerWithVersion()
    {
        var (formatter, writer) = GivenFormatterWithCapture();

        formatter.DisplayBanner("2.0.1");

        var text = writer.ToString();
        text.Should().Contain("2.0.1");
    }

    [Test]
    public void WriteAnalysisReportWithModuleName()
    {
        var dockerItem = new CleanableItemBuilder()
            .ForModule(CleanupModuleName.Docker)
            .Safe()
            .WithReason("Docker cache")
            .Small()
            .Build();
        var dockerAnalysis = ModuleAnalysis.Create(CleanupModuleName.Docker, [dockerItem]).Value;
        var report = AnalysisReport.Create([dockerAnalysis]).Value;
        var (formatter, writer) = GivenFormatterWithCapture();

        formatter.DisplayAnalysisReport(report);

        var text = writer.ToString();
        text.Should().Contain("Docker");
    }

    [Test]
    public void WriteCompletionWithModuleName()
    {
        var cleanupResult = new CleanupResultBuilder()
            .WithFilesDeleted(3)
            .WithBytesFreed(2048)
            .Build();
        var summary = CleanupSummary.Create(
            CleanupModuleName.Docker,
            [new CleanableItemBuilder().ForModule(CleanupModuleName.Docker).Safe().WithReason("cache").Build()],
            cleanupResult).Value;
        var (formatter, writer) = GivenFormatterWithCapture();

        formatter.DisplayCompletion([summary]);

        var text = writer.ToString();
        text.Should().Contain("Docker");
    }
}

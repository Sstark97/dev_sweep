using AwesomeAssertions;
using DevSweep.Application.Models;
using DevSweep.Domain.Entities;
using DevSweep.Domain.Enums;
using DevSweep.Infrastructure.Cli.Output;
using DevSweep.Tests.Builders;

namespace DevSweep.Tests.Infrastructure.Cli.Output;

internal sealed class PlainTextOutputFormatterShould
{
    [Test]
    public void WriteInfoMessageWithPrefix()
    {
        var output = new StringWriter();
        var formatter = new PlainTextOutputFormatter(output);

        formatter.Info("Hello world");

        var text = output.ToString();
        text.Should().Contain("[INFO]");
        text.Should().Contain("Hello world");
    }

    [Test]
    public void WriteSuccessMessageWithPrefix()
    {
        var output = new StringWriter();
        var formatter = new PlainTextOutputFormatter(output);

        formatter.Success("Done");

        var text = output.ToString();
        text.Should().Contain("[OK]");
        text.Should().Contain("Done");
    }

    [Test]
    public void WriteWarningMessageWithPrefix()
    {
        var output = new StringWriter();
        var formatter = new PlainTextOutputFormatter(output);

        formatter.Warning("Watch out");

        var text = output.ToString();
        text.Should().Contain("[WARN]");
        text.Should().Contain("Watch out");
    }

    [Test]
    public void WriteErrorMessageWithPrefix()
    {
        var output = new StringWriter();
        var formatter = new PlainTextOutputFormatter(output);

        formatter.Error("Something failed");

        var text = output.ToString();
        text.Should().Contain("[ERROR]");
        text.Should().Contain("Something failed");
    }

    [Test]
    public void WriteDebugMessageWithPrefix()
    {
        var output = new StringWriter();
        var formatter = new PlainTextOutputFormatter(output);

        formatter.Debug("Verbose detail");

        var text = output.ToString();
        text.Should().Contain("[DEBUG]");
        text.Should().Contain("Verbose detail");
    }

    [Test]
    public void WriteSectionWithUnderline()
    {
        var output = new StringWriter();
        var formatter = new PlainTextOutputFormatter(output);

        formatter.Section("My Section");

        var text = output.ToString();
        text.Should().Contain("My Section");
        text.Should().Contain("----------");
    }

    [Test]
    public void WriteBannerWithVersion()
    {
        var output = new StringWriter();
        var formatter = new PlainTextOutputFormatter(output);

        formatter.DisplayBanner("2.0.1");

        var text = output.ToString();
        text.Should().Contain("DevSweep");
        text.Should().Contain("2.0.1");
    }

    [Test]
    public void WriteAnalysisReportWithModuleBreakdown()
    {
        var dockerItem = new CleanableItemBuilder()
            .ForModule(CleanupModuleName.Docker)
            .Safe()
            .WithReason("Docker cache")
            .Small()
            .Build();
        var dockerAnalysis = ModuleAnalysis.Create(CleanupModuleName.Docker, [dockerItem]).Value;
        var report = AnalysisReport.Create([dockerAnalysis]).Value;

        var output = new StringWriter();
        var formatter = new PlainTextOutputFormatter(output);

        formatter.DisplayAnalysisReport(report);

        var text = output.ToString();
        text.Should().Contain("Docker");
        text.Should().Contain("1");
    }

    [Test]
    public void WriteCompletionWithSummaryTotals()
    {
        var cleanupResult = new CleanupResultBuilder()
            .WithFilesDeleted(3)
            .WithBytesFreed(2048)
            .Build();
        var summary = CleanupSummary.Create(
            CleanupModuleName.Docker,
            [new CleanableItemBuilder().ForModule(CleanupModuleName.Docker).Safe().WithReason("cache").Build()],
            cleanupResult).Value;

        var output = new StringWriter();
        var formatter = new PlainTextOutputFormatter(output);

        formatter.DisplayCompletion([summary]);

        var text = output.ToString();
        text.Should().Contain("Docker");
        text.Should().Contain("3");
    }
}

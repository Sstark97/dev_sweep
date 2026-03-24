using System.Text.Json;
using AwesomeAssertions;
using DevSweep.Application.Models;
using DevSweep.Domain.Entities;
using DevSweep.Domain.Enums;
using DevSweep.Infrastructure.Cli.Output;
using DevSweep.Infrastructure.Cli.Output.JsonModels;
using DevSweep.Tests.Builders;

namespace DevSweep.Tests.Infrastructure.Cli.Output;

internal sealed class JsonOutputFormatterShould
{
    [Test]
    public void WriteInfoAsJsonLine()
    {
        var output = new StringWriter();
        var formatter = new JsonOutputFormatter(output);

        formatter.Info("Hello world");

        var text = output.ToString().Trim();
        text.Should().Contain("\"level\"");
        text.Should().Contain("\"info\"");
        text.Should().Contain("Hello world");
    }

    [Test]
    public void WriteErrorAsJsonLine()
    {
        var output = new StringWriter();
        var formatter = new JsonOutputFormatter(output);

        formatter.Error("Something failed");

        var text = output.ToString().Trim();
        text.Should().Contain("\"error\"");
        text.Should().Contain("Something failed");
    }

    [Test]
    public void WriteBannerAsJsonEvent()
    {
        var output = new StringWriter();
        var formatter = new JsonOutputFormatter(output);

        formatter.DisplayBanner("2.0.1");

        var text = output.ToString().Trim();
        text.Should().Contain("\"banner\"");
        text.Should().Contain("2.0.1");
    }

    [Test]
    public void WriteAnalysisReportAsStructuredJson()
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
        var formatter = new JsonOutputFormatter(output);

        formatter.DisplayAnalysisReport(report);

        var text = output.ToString().Trim();
        text.Should().Contain("Docker");
        text.Should().NotBeNullOrWhiteSpace();
    }

    [Test]
    public void WriteCompletionAsStructuredJson()
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
        var formatter = new JsonOutputFormatter(output);

        formatter.DisplayCompletion([summary]);

        var text = output.ToString().Trim();
        text.Should().Contain("Docker");
        text.Should().NotBeNullOrWhiteSpace();
    }

    [Test]
    public void ProduceValidJsonForAnalysisReport()
    {
        var dockerItem = new CleanableItemBuilder()
            .ForModule(CleanupModuleName.Docker)
            .Safe()
            .WithReason("Docker cache")
            .Small()
            .Build();
        var homebrewItem = new CleanableItemBuilder()
            .ForModule(CleanupModuleName.Homebrew)
            .Unsafe()
            .WithReason("In use")
            .Large()
            .Build();
        var dockerAnalysis = ModuleAnalysis.Create(CleanupModuleName.Docker, [dockerItem]).Value;
        var homebrewAnalysis = ModuleAnalysis.Create(CleanupModuleName.Homebrew, [homebrewItem]).Value;
        var report = AnalysisReport.Create([dockerAnalysis, homebrewAnalysis]).Value;

        var output = new StringWriter();
        var formatter = new JsonOutputFormatter(output);

        formatter.DisplayAnalysisReport(report);

        var text = output.ToString().Trim();
        var dto = JsonSerializer.Deserialize(text, DevSweepJsonContext.Default.AnalysisReportJson);

        dto.Should().NotBeNull();
        dto!.ModuleCount.Should().Be(2);
        dto.TotalItems.Should().Be(2);
        dto.Modules.Should().HaveCount(2);
    }

    [Test]
    public void ProduceValidJsonForCompletion()
    {
        var cleanupResult = new CleanupResultBuilder()
            .WithFilesDeleted(5)
            .WithBytesFreed(4096)
            .Build();
        var summary = CleanupSummary.Create(
            CleanupModuleName.Homebrew,
            [new CleanableItemBuilder().ForModule(CleanupModuleName.Homebrew).Safe().WithReason("cache").Build()],
            cleanupResult).Value;

        var output = new StringWriter();
        var formatter = new JsonOutputFormatter(output);

        formatter.DisplayCompletion([summary]);

        var text = output.ToString().Trim();
        var dto = JsonSerializer.Deserialize(text, DevSweepJsonContext.Default.CleanupCompletionJson);

        dto.Should().NotBeNull();
        dto!.TotalFilesDeleted.Should().Be(5);
        dto.Modules.Should().HaveCount(1);
        dto.Modules[0].Name.Should().Be("Homebrew");
        dto.Modules[0].FilesDeleted.Should().Be(5);
    }
}

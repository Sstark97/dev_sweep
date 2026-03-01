using DevSweep.Application.Models;
using DevSweep.Domain.Enums;
using DevSweep.Tests.Builders;

namespace DevSweep.Tests.Application.Models;

public class AnalysisReportShould
{
    [Fact]
    public void SucceedWithValidModuleAnalyses()
    {
        var dockerAnalysis = ModuleAnalysis.CreateEmpty(CleanupModuleName.Docker);

        var result = AnalysisReport.Create([dockerAnalysis]);

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public void SucceedWithEmptyModuleAnalysesList()
    {
        var result = AnalysisReport.Create([]);

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public void CalculateTotalSizeAcrossModules()
    {
        var dockerItem = new CleanableItemBuilder()
            .ForModule(CleanupModuleName.Docker)
            .Safe()
            .WithReason("Docker cache")
            .Small()
            .Build();
        var homebrewItem = new CleanableItemBuilder()
            .ForModule(CleanupModuleName.Homebrew)
            .Safe()
            .WithReason("Homebrew cache")
            .Large()
            .Build();

        var dockerAnalysis = ModuleAnalysis.Create(CleanupModuleName.Docker, [dockerItem]).Value;
        var homebrewAnalysis = ModuleAnalysis.Create(CleanupModuleName.Homebrew, [homebrewItem]).Value;

        var reportResult = AnalysisReport.Create([dockerAnalysis, homebrewAnalysis]);
        var report = reportResult.Value;

        reportResult.IsSuccess.Should().BeTrue();
        report.TotalSize().Should().Be(dockerItem.Size.Add(homebrewItem.Size));
    }

    [Fact]
    public void CountTotalItemsAcrossModules()
    {
        var dockerItem = new CleanableItemBuilder()
            .ForModule(CleanupModuleName.Docker)
            .Safe()
            .WithReason("Docker cache")
            .Build();
        var homebrewItem = new CleanableItemBuilder()
            .ForModule(CleanupModuleName.Homebrew)
            .Safe()
            .WithReason("Homebrew cache")
            .Build();
        var anotherHomebrewItem = new CleanableItemBuilder()
            .ForModule(CleanupModuleName.Homebrew)
            .Safe()
            .WithReason("Another cache")
            .Build();

        var dockerAnalysis = ModuleAnalysis.Create(CleanupModuleName.Docker, [dockerItem]).Value;
        var homebrewAnalysis = ModuleAnalysis.Create(CleanupModuleName.Homebrew, [homebrewItem, anotherHomebrewItem]).Value;

        var reportResult = AnalysisReport.Create([dockerAnalysis, homebrewAnalysis]);
        var report = reportResult.Value;

        reportResult.IsSuccess.Should().BeTrue();
        report.TotalItemCount().Should().Be(3);
    }

    [Fact]
    public void CountModules()
    {
        var dockerAnalysis = ModuleAnalysis.CreateEmpty(CleanupModuleName.Docker);
        var homebrewAnalysis = ModuleAnalysis.CreateEmpty(CleanupModuleName.Homebrew);

        var reportResult = AnalysisReport.Create([dockerAnalysis, homebrewAnalysis]);
        var report = reportResult.Value;

        reportResult.IsSuccess.Should().BeTrue();
        report.ModuleCount().Should().Be(2);
    }

    [Fact]
    public void ReportEmptyWhenAllModulesEmpty()
    {
        var emptyDockerAnalysis = ModuleAnalysis.CreateEmpty(CleanupModuleName.Docker);
        var emptyHomebrewAnalysis = ModuleAnalysis.CreateEmpty(CleanupModuleName.Homebrew);

        var reportResult = AnalysisReport.Create([emptyDockerAnalysis, emptyHomebrewAnalysis]);
        var report = reportResult.Value;

        reportResult.IsSuccess.Should().BeTrue();
        report.IsEmpty().Should().BeTrue();
    }

    [Fact]
    public void ReportNotEmptyWhenAnyModuleHasItems()
    {
        var dockerItem = new CleanableItemBuilder()
            .ForModule(CleanupModuleName.Docker)
            .Safe()
            .WithReason("Docker cache")
            .Build();

        var dockerAnalysis = ModuleAnalysis.Create(CleanupModuleName.Docker, [dockerItem]).Value;
        var emptyHomebrewAnalysis = ModuleAnalysis.CreateEmpty(CleanupModuleName.Homebrew);

        var reportResult = AnalysisReport.Create([dockerAnalysis, emptyHomebrewAnalysis]);
        var report = reportResult.Value;

        reportResult.IsSuccess.Should().BeTrue();
        report.IsEmpty().Should().BeFalse();
    }

    [Fact]
    public void CreateEmptyReport()
    {
        var emptyReport = AnalysisReport.CreateEmpty();

        emptyReport.IsEmpty().Should().BeTrue();
        emptyReport.ModuleCount().Should().Be(0);
        emptyReport.TotalItemCount().Should().Be(0);
    }

    [Fact]
    public void FailWhenModuleAnalysesIsNull()
    {
        var result = AnalysisReport.Create(null);

        result.IsFailure.Should().BeTrue();
        result.Error.IsValidationError().Should().BeTrue();
    }
}

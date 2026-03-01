using DevSweep.Application.Models;
using DevSweep.Domain.Enums;
using DevSweep.Tests.Builders;

namespace DevSweep.Tests.Application.Models;

public class ModuleAnalysisShould
{
    [Fact]
    public void SucceedWithValidItems()
    {
        var dockerItem = new CleanableItemBuilder()
            .ForModule(CleanupModuleName.Docker)
            .Safe()
            .WithReason("Docker cache")
            .Build();

        var result = ModuleAnalysis.Create(CleanupModuleName.Docker, [dockerItem]);

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public void SucceedWithEmptyItemsList()
    {
        var result = ModuleAnalysis.Create(CleanupModuleName.Docker, []);

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public void CalculateTotalSizeFromAllItems()
    {
        var firstItem = new CleanableItemBuilder()
            .ForModule(CleanupModuleName.Docker)
            .Safe()
            .WithReason("Docker layer cache")
            .Small()
            .Build();
        var secondItem = new CleanableItemBuilder()
            .ForModule(CleanupModuleName.Docker)
            .Safe()
            .WithReason("Docker build cache")
            .Large()
            .Build();

        var analysisResult = ModuleAnalysis.Create(CleanupModuleName.Docker, [firstItem, secondItem]);
        var analysis = analysisResult.Value;

        analysisResult.IsSuccess.Should().BeTrue();
        analysis.TotalSize().Should().Be(firstItem.Size.Add(secondItem.Size));
    }

    [Fact]
    public void CountSafeItems()
    {
        var safeItem = new CleanableItemBuilder()
            .ForModule(CleanupModuleName.Docker)
            .Safe()
            .WithReason("Old cache")
            .Build();
        var unsafeItem = new CleanableItemBuilder()
            .ForModule(CleanupModuleName.Docker)
            .Unsafe()
            .WithReason("Currently used")
            .Build();
        var anotherSafeItem = new CleanableItemBuilder()
            .ForModule(CleanupModuleName.Docker)
            .Safe()
            .WithReason("Stale layer")
            .Build();

        var analysisResult = ModuleAnalysis.Create(CleanupModuleName.Docker, [safeItem, unsafeItem, anotherSafeItem]);
        var analysis = analysisResult.Value;

        analysisResult.IsSuccess.Should().BeTrue();
        analysis.SafeItemCount().Should().Be(2);
    }

    [Fact]
    public void CountUnsafeItems()
    {
        var safeItem = new CleanableItemBuilder()
            .ForModule(CleanupModuleName.Docker)
            .Safe()
            .WithReason("Old cache")
            .Build();
        var runningContainerItem = new CleanableItemBuilder()
            .ForModule(CleanupModuleName.Docker)
            .Unsafe()
            .WithReason("Running container")
            .Build();

        var analysisResult = ModuleAnalysis.Create(CleanupModuleName.Docker, [safeItem, runningContainerItem]);
        var analysis = analysisResult.Value;

        analysisResult.IsSuccess.Should().BeTrue();
        analysis.UnsafeItemCount().Should().Be(1);
    }

    [Fact]
    public void CountTotalItems()
    {
        var safeItem = new CleanableItemBuilder()
            .ForModule(CleanupModuleName.Docker)
            .Safe()
            .WithReason("Old cache")
            .Build();
        var unsafeItem = new CleanableItemBuilder()
            .ForModule(CleanupModuleName.Docker)
            .Unsafe()
            .WithReason("Running container")
            .Build();

        var analysisResult = ModuleAnalysis.Create(CleanupModuleName.Docker, [safeItem, unsafeItem]);
        var analysis = analysisResult.Value;

        analysisResult.IsSuccess.Should().BeTrue();
        analysis.ItemCount().Should().Be(2);
    }

    [Fact]
    public void ReportEmptyWhenNoItems()
    {
        var analysisResult = ModuleAnalysis.Create(CleanupModuleName.Docker, []);
        var analysis = analysisResult.Value;

        analysisResult.IsSuccess.Should().BeTrue();
        analysis.IsEmpty().Should().BeTrue();
    }

    [Fact]
    public void ReportNotEmptyWhenHasItems()
    {
        var dockerItem = new CleanableItemBuilder()
            .ForModule(CleanupModuleName.Docker)
            .Safe()
            .WithReason("Docker cache")
            .Build();

        var analysisResult = ModuleAnalysis.Create(CleanupModuleName.Docker, [dockerItem]);
        var analysis = analysisResult.Value;

        analysisResult.IsSuccess.Should().BeTrue();
        analysis.IsEmpty().Should().BeFalse();
    }

    [Fact]
    public void CreateEmptyAnalysis()
    {
        var emptyAnalysis = ModuleAnalysis.CreateEmpty(CleanupModuleName.Docker);

        emptyAnalysis.IsEmpty().Should().BeTrue();
        emptyAnalysis.Module.Should().Be(CleanupModuleName.Docker);
    }

    [Fact]
    public void FailWhenItemsIsNull()
    {
        var result = ModuleAnalysis.Create(CleanupModuleName.Docker, null);

        result.IsFailure.Should().BeTrue();
        result.Error.IsValidationError().Should().BeTrue();
    }
}

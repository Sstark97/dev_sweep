using DevSweep.Domain.Entities;
using DevSweep.Domain.Enums;
using DevSweep.Domain.ValueObjects;
using DevSweep.Tests.Builders;

namespace DevSweep.Tests.Domain.Entities;

public class CleanupSummaryShould
{
    [Fact]
    public void FailWhenItemsListIsEmpty()
    {
        var emptyItems = new List<CleanableItem>();

        var result = CleanupSummary.Create(
            CleanupModuleName.Docker,
            emptyItems,
            new CleanupResultBuilder().Empty().Build());

        result.IsFailure.Should().BeTrue();
        result.Error.IsInvalidOperationError().Should().BeTrue();
    }

    [Fact]
    public void SucceedWithValidItems()
    {
        var safeItem = new CleanableItemBuilder()
            .ForModule(CleanupModuleName.Docker)
            .Safe()
            .WithReason("Project dependencies")
            .Build();
        var items = new List<CleanableItem> { safeItem };

        var result = CleanupSummary.Create(
            CleanupModuleName.Docker,
            items,
            new CleanupResultBuilder().WithFilesDeleted(1).Build());

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public void CountTotalItemsScanned()
    {
        var firstItem = new CleanableItemBuilder()
            .ForModule(CleanupModuleName.Docker)
            .Safe()
            .WithReason("Project dependencies")
            .Build();
        var secondItem = new CleanableItemBuilder()
            .ForModule(CleanupModuleName.Docker)
            .Unsafe()
            .WithReason("Currently in use")
            .Build();
        var thirdItem = new CleanableItemBuilder()
            .ForModule(CleanupModuleName.Docker)
            .Safe()
            .WithReason("Old cache")
            .Build();
        var items = new List<CleanableItem> { firstItem, secondItem, thirdItem };
        var cleanupResult = new CleanupResultBuilder().WithFilesDeleted(2).Build();

        var result = CleanupSummary.Create(
            CleanupModuleName.Docker,
            items,
            cleanupResult);

        var summary = result.Value;
        summary.TotalItemsScanned.Should().Be(3);
    }

    [Fact]
    public void CountOnlySafeItems()
    {
        var safeItem = new CleanableItemBuilder()
            .ForModule(CleanupModuleName.Docker)
            .Safe()
            .WithReason("Project dependencies")
            .Build();
        var unsafeItem = new CleanableItemBuilder()
            .ForModule(CleanupModuleName.Docker)
            .Unsafe()
            .WithReason("Currently in use")
            .Build();
        var anotherSafeItem = new CleanableItemBuilder()
            .ForModule(CleanupModuleName.Docker)
            .Safe()
            .WithReason("Old cache")
            .Build();
        var items = new List<CleanableItem> { safeItem, unsafeItem, anotherSafeItem };
        var cleanupResult = new CleanupResultBuilder().WithFilesDeleted(2).Build();

        var result = CleanupSummary.Create(
            CleanupModuleName.Docker,
            items,
            cleanupResult);

        var summary = result.Value;
        summary.SafeItemsFound.Should().Be(2);
    }

    [Fact]
    public void CountZeroSafeItemsWhenAllUnsafe()
    {
        var firstUnsafeItem = new CleanableItemBuilder()
            .ForModule(CleanupModuleName.Docker)
            .Unsafe()
            .WithReason("Currently in use")
            .Build();
        var secondUnsafeItem = new CleanableItemBuilder()
            .ForModule(CleanupModuleName.Docker)
            .Unsafe()
            .WithReason("System dependency")
            .Build();
        var items = new List<CleanableItem> { firstUnsafeItem, secondUnsafeItem };
        var cleanupResult = new CleanupResultBuilder().Empty().Build();

        var result = CleanupSummary.Create(
            CleanupModuleName.Docker,
            items,
            cleanupResult);

        var summary = result.Value;
        summary.SafeItemsFound.Should().Be(0);
    }

    [Fact]
    public void PreserveCleanupResult()
    {
        var safeItem = new CleanableItemBuilder()
            .ForModule(CleanupModuleName.Docker)
            .Safe()
            .WithReason("Project dependencies")
            .Build();
        var items = new List<CleanableItem> { safeItem };
        var originalCleanupResult = new CleanupResultBuilder().WithFilesDeleted(1).Build();

        var result = CleanupSummary.Create(
            CleanupModuleName.Docker,
            items,
            originalCleanupResult);

        var summary = result.Value;
        summary.Result.Should().Be(originalCleanupResult);
    }

    [Fact]
    public void PreserveModuleName()
    {
        var safeItem = new CleanableItemBuilder()
            .ForModule(CleanupModuleName.Projects)
            .Safe()
            .WithReason("Project dependencies")
            .Build();
        var items = new List<CleanableItem> { safeItem };
        var cleanupResult = new CleanupResultBuilder().WithFilesDeleted(1).Build();

        var result = CleanupSummary.Create(
            CleanupModuleName.Projects,
            items,
            cleanupResult);

        var summary = result.Value;
        summary.Module.Should().Be(CleanupModuleName.Projects);
    }

    [Fact]
    public void FailWhenItemsIsNull()
    {
        var result = CleanupSummary.Create(
            CleanupModuleName.Docker,
            null,
            new CleanupResultBuilder().Empty().Build());

        result.IsFailure.Should().BeTrue();
        result.Error.IsValidationError().Should().BeTrue();
    }
}

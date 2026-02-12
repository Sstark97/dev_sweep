using DevSweep.Domain.Entities;
using DevSweep.Domain.Enums;
using DevSweep.Domain.ValueObjects;

namespace DevSweep.Tests.Domain.Entities;

public class CleanupSummaryShould
{
    [Fact]
    public void FailWhenItemsListIsEmpty()
    {
        var emptyItems = new List<CleanableItem>();
        var sizeResult = FileSize.Create(0);
        var cleanupResult = CleanupResult.Create(0, sizeResult.Value);

        var result = CleanupSummary.Create(
            CleanupModuleName.Docker,
            emptyItems,
            cleanupResult.Value);

        result.IsFailure.Should().BeTrue();
        result.Error.IsInvalidOperationError().Should().BeTrue();
    }

    [Fact]
    public void SucceedWithValidItems()
    {
        var pathResult = FilePath.Create("/test/file.txt");
        var sizeResult = FileSize.Create(1024);
        var safeItem = CleanableItem.CreateSafe(
            pathResult.Value,
            sizeResult.Value,
            CleanupModuleName.Docker,
            "Project dependencies");
        var items = new List<CleanableItem> { safeItem };
        var cleanupResult = CleanupResult.Create(1, sizeResult.Value);

        var result = CleanupSummary.Create(
            CleanupModuleName.Docker,
            items,
            cleanupResult.Value);

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public void CountTotalItemsScanned()
    {
        var pathResult = FilePath.Create("/test/file.txt");
        var sizeResult = FileSize.Create(1024);
        var firstItem = CleanableItem.CreateSafe(
            pathResult.Value,
            sizeResult.Value,
            CleanupModuleName.Docker,
            "Project dependencies");
        var secondItem = CleanableItem.CreateUnsafe(
            pathResult.Value,
            sizeResult.Value,
            CleanupModuleName.Docker,
            "Currently in use");
        var thirdItem = CleanableItem.CreateSafe(
            pathResult.Value,
            sizeResult.Value,
            CleanupModuleName.Docker,
            "Old cache");
        var items = new List<CleanableItem> { firstItem, secondItem, thirdItem };
        var cleanupResult = CleanupResult.Create(2, sizeResult.Value);

        var result = CleanupSummary.Create(
            CleanupModuleName.Docker,
            items,
            cleanupResult.Value);

        var summary = result.Value;
        summary.TotalItemsScanned.Should().Be(3);
    }

    [Fact]
    public void CountOnlySafeItems()
    {
        var pathResult = FilePath.Create("/test/file.txt");
        var sizeResult = FileSize.Create(1024);
        var safeItem = CleanableItem.CreateSafe(
            pathResult.Value,
            sizeResult.Value,
            CleanupModuleName.Docker,
            "Project dependencies");
        var unsafeItem = CleanableItem.CreateUnsafe(
            pathResult.Value,
            sizeResult.Value,
            CleanupModuleName.Docker,
            "Currently in use");
        var anotherSafeItem = CleanableItem.CreateSafe(
            pathResult.Value,
            sizeResult.Value,
            CleanupModuleName.Docker,
            "Old cache");
        var items = new List<CleanableItem> { safeItem, unsafeItem, anotherSafeItem };
        var cleanupResult = CleanupResult.Create(2, sizeResult.Value);

        var result = CleanupSummary.Create(
            CleanupModuleName.Docker,
            items,
            cleanupResult.Value);

        var summary = result.Value;
        summary.SafeItemsFound.Should().Be(2);
    }

    [Fact]
    public void CountZeroSafeItemsWhenAllUnsafe()
    {
        var pathResult = FilePath.Create("/test/file.txt");
        var sizeResult = FileSize.Create(1024);
        var firstUnsafeItem = CleanableItem.CreateUnsafe(
            pathResult.Value,
            sizeResult.Value,
            CleanupModuleName.Docker,
            "Currently in use");
        var secondUnsafeItem = CleanableItem.CreateUnsafe(
            pathResult.Value,
            sizeResult.Value,
            CleanupModuleName.Docker,
            "System dependency");
        var items = new List<CleanableItem> { firstUnsafeItem, secondUnsafeItem };
        var cleanupResult = CleanupResult.Create(0, sizeResult.Value);

        var result = CleanupSummary.Create(
            CleanupModuleName.Docker,
            items,
            cleanupResult.Value);

        var summary = result.Value;
        summary.SafeItemsFound.Should().Be(0);
    }

    [Fact]
    public void PreserveCleanupResult()
    {
        var pathResult = FilePath.Create("/test/file.txt");
        var sizeResult = FileSize.Create(1024);
        var safeItem = CleanableItem.CreateSafe(
            pathResult.Value,
            sizeResult.Value,
            CleanupModuleName.Docker,
            "Project dependencies");
        var items = new List<CleanableItem> { safeItem };
        var originalCleanupResult = CleanupResult.Create(1, sizeResult.Value).Value;

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
        var pathResult = FilePath.Create("/test/file.txt");
        var sizeResult = FileSize.Create(1024);
        var safeItem = CleanableItem.CreateSafe(
            pathResult.Value,
            sizeResult.Value,
            CleanupModuleName.Projects,
            "Project dependencies");
        var items = new List<CleanableItem> { safeItem };
        var cleanupResult = CleanupResult.Create(1, sizeResult.Value);

        var result = CleanupSummary.Create(
            CleanupModuleName.Projects,
            items,
            cleanupResult.Value);

        var summary = result.Value;
        summary.Module.Should().Be(CleanupModuleName.Projects);
    }
}

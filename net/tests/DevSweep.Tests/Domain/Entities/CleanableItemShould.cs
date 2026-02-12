using DevSweep.Domain.Entities;
using DevSweep.Domain.Enums;
using DevSweep.Domain.ValueObjects;

namespace DevSweep.Tests.Domain.Entities;

public class CleanableItemShould
{
    [Fact]
    public void CreateSafeItemWithReason()
    {
        var pathResult = FilePath.Create("/test/file.txt");
        var sizeResult = FileSize.Create(1024);
        pathResult.IsSuccess.Should().BeTrue();
        sizeResult.IsSuccess.Should().BeTrue();

        var safeItem = CleanableItem.CreateSafe(
            pathResult.Value,
            sizeResult.Value,
            CleanupModuleName.Docker,
            "Project dependencies");

        safeItem.IsSafeToDelete.Should().BeTrue();
        safeItem.Reason.Should().Be("Project dependencies");
    }

    [Fact]
    public void CreateUnsafeItemWithReason()
    {
        var pathResult = FilePath.Create("/test/file.txt");
        var sizeResult = FileSize.Create(1024);

        var unsafeItem = CleanableItem.CreateUnsafe(
            pathResult.Value,
            sizeResult.Value,
            CleanupModuleName.Docker,
            "Currently in use");

        unsafeItem.IsSafeToDelete.Should().BeFalse();
        unsafeItem.Reason.Should().Be("Currently in use");
    }

    [Fact]
    public void FailToMarkForDeletionWhenNotSafe()
    {
        var pathResult = FilePath.Create("/test/file.txt");
        var sizeResult = FileSize.Create(1024);

        var unsafeItem = CleanableItem.CreateUnsafe(
            pathResult.Value,
            sizeResult.Value,
            CleanupModuleName.Docker,
            "Currently in use");

        var result = unsafeItem.MarkForDeletion();

        result.IsFailure.Should().BeTrue();
        result.Error.IsInvalidOperationError().Should().BeTrue();
    }

    [Fact]
    public void SucceedToMarkForDeletionWhenSafe()
    {
        var pathResult = FilePath.Create("/test/file.txt");
        var sizeResult = FileSize.Create(1024);

        var safeItem = CleanableItem.CreateSafe(
            pathResult.Value,
            sizeResult.Value,
            CleanupModuleName.Docker,
            "Project dependencies");

        var result = safeItem.MarkForDeletion();

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public void FailToMarkAsUnsafeWhenAlreadyUnsafe()
    {
        var pathResult = FilePath.Create("/test/file.txt");
        var sizeResult = FileSize.Create(1024);

        var unsafeItem = CleanableItem.CreateUnsafe(
            pathResult.Value,
            sizeResult.Value,
            CleanupModuleName.Docker,
            "Currently in use");

        var result = unsafeItem.MarkAsUnsafe("Another reason");

        result.IsFailure.Should().BeTrue();
        result.Error.IsInvalidOperationError().Should().BeTrue();
    }

    [Fact]
    public void SucceedToMarkAsUnsafeWhenSafe()
    {
        var pathResult = FilePath.Create("/test/file.txt");
        var sizeResult = FileSize.Create(1024);

        var safeItem = CleanableItem.CreateSafe(
            pathResult.Value,
            sizeResult.Value,
            CleanupModuleName.Docker,
            "Project dependencies");

        var result = safeItem.MarkAsUnsafe("Found in active container");

        result.IsSuccess.Should().BeTrue();
        var updatedItem = result.Value;
        updatedItem.IsSafeToDelete.Should().BeFalse();
        updatedItem.Reason.Should().Be("Found in active container");
    }

    [Fact]
    public void FailToMarkAsSafeWhenAlreadySafe()
    {
        var pathResult = FilePath.Create("/test/file.txt");
        var sizeResult = FileSize.Create(1024);

        var safeItem = CleanableItem.CreateSafe(
            pathResult.Value,
            sizeResult.Value,
            CleanupModuleName.Docker,
            "Project dependencies");

        var result = safeItem.MarkAsSafe("Another reason");

        result.IsFailure.Should().BeTrue();
        result.Error.IsInvalidOperationError().Should().BeTrue();
    }

    [Fact]
    public void SucceedToMarkAsSafeWhenUnsafe()
    {
        var pathResult = FilePath.Create("/test/file.txt");
        var sizeResult = FileSize.Create(1024);

        var unsafeItem = CleanableItem.CreateUnsafe(
            pathResult.Value,
            sizeResult.Value,
            CleanupModuleName.Docker,
            "Currently in use");

        var result = unsafeItem.MarkAsSafe("Container stopped");

        result.IsSuccess.Should().BeTrue();
        var updatedItem = result.Value;
        updatedItem.IsSafeToDelete.Should().BeTrue();
        updatedItem.Reason.Should().Be("Container stopped");
    }

    [Fact]
    public void PreservePathWhenMarkingAsSafe()
    {
        var pathResult = FilePath.Create("/test/file.txt");
        var sizeResult = FileSize.Create(1024);
        var originalPath = pathResult.Value;

        var unsafeItem = CleanableItem.CreateUnsafe(
            originalPath,
            sizeResult.Value,
            CleanupModuleName.Docker,
            "Currently in use");

        var result = unsafeItem.MarkAsSafe("Container stopped");

        result.Value.Path.Should().Be(originalPath);
    }

    [Fact]
    public void PreserveSizeWhenMarkingAsUnsafe()
    {
        var pathResult = FilePath.Create("/test/file.txt");
        var sizeResult = FileSize.Create(1024);
        var originalSize = sizeResult.Value;

        var safeItem = CleanableItem.CreateSafe(
            pathResult.Value,
            originalSize,
            CleanupModuleName.Docker,
            "Project dependencies");

        var result = safeItem.MarkAsUnsafe("Found in active container");

        result.Value.Size.Should().Be(originalSize);
    }

    [Fact]
    public void PreserveModuleTypeWhenChangingSafety()
    {
        var pathResult = FilePath.Create("/test/file.txt");
        var sizeResult = FileSize.Create(1024);

        var safeItem = CleanableItem.CreateSafe(
            pathResult.Value,
            sizeResult.Value,
            CleanupModuleName.Projects,
            "Project dependencies");

        var result = safeItem.MarkAsUnsafe("Found in active project");

        result.Value.ModuleType.Should().Be(CleanupModuleName.Projects);
    }
}

using DevSweep.Domain.Entities;
using DevSweep.Domain.Enums;
using DevSweep.Domain.ValueObjects;
using DevSweep.Tests.Builders;

namespace DevSweep.Tests.Domain.Entities;

public class CleanableItemShould
{
    [Fact]
    public void CreateSafeItemWithReason()
    {
        var safeItem = new CleanableItemBuilder()
            .ForModule(CleanupModuleName.Docker)
            .Safe()
            .WithReason("Project dependencies")
            .Build();

        safeItem.IsSafeToDelete.Should().BeTrue();
        safeItem.Reason.Should().Be("Project dependencies");
    }

    [Fact]
    public void CreateUnsafeItemWithReason()
    {
        var unsafeItem = new CleanableItemBuilder()
            .ForModule(CleanupModuleName.Docker)
            .Unsafe()
            .WithReason("Currently in use")
            .Build();

        unsafeItem.IsSafeToDelete.Should().BeFalse();
        unsafeItem.Reason.Should().Be("Currently in use");
    }

    [Fact]
    public void FailToMarkForDeletionWhenNotSafe()
    {
        var unsafeItem = new CleanableItemBuilder()
            .ForModule(CleanupModuleName.Docker)
            .Unsafe()
            .WithReason("Currently in use")
            .Build();

        var result = unsafeItem.MarkForDeletion();

        result.IsFailure.Should().BeTrue();
        result.Error.IsInvalidOperationError().Should().BeTrue();
    }

    [Fact]
    public void SucceedToMarkForDeletionWhenSafe()
    {
        var safeItem = new CleanableItemBuilder()
            .ForModule(CleanupModuleName.Docker)
            .Safe()
            .WithReason("Project dependencies")
            .Build();

        var result = safeItem.MarkForDeletion();

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public void FailToMarkAsUnsafeWhenAlreadyUnsafe()
    {
        var unsafeItem = new CleanableItemBuilder()
            .ForModule(CleanupModuleName.Docker)
            .Unsafe()
            .WithReason("Currently in use")
            .Build();

        var result = unsafeItem.MarkAsUnsafe("Another reason");

        result.IsFailure.Should().BeTrue();
        result.Error.IsInvalidOperationError().Should().BeTrue();
    }

    [Fact]
    public void SucceedToMarkAsUnsafeWhenSafe()
    {
        var safeItem = new CleanableItemBuilder()
            .ForModule(CleanupModuleName.Docker)
            .Safe()
            .WithReason("Project dependencies")
            .Build();

        var result = safeItem.MarkAsUnsafe("Found in active container");
        var updatedItem = result.Value;
        
        result.IsSuccess.Should().BeTrue();
        updatedItem.IsSafeToDelete.Should().BeFalse();
        updatedItem.Reason.Should().Be("Found in active container");
    }

    [Fact]
    public void FailToMarkAsSafeWhenAlreadySafe()
    {
        var safeItem = new CleanableItemBuilder()
            .ForModule(CleanupModuleName.Docker)
            .Safe()
            .WithReason("Project dependencies")
            .Build();

        var result = safeItem.MarkAsSafe("Another reason");

        result.IsFailure.Should().BeTrue();
        result.Error.IsInvalidOperationError().Should().BeTrue();
    }

    [Fact]
    public void SucceedToMarkAsSafeWhenUnsafe()
    {
        var unsafeItem = new CleanableItemBuilder()
            .ForModule(CleanupModuleName.Docker)
            .Unsafe()
            .WithReason("Currently in use")
            .Build();

        var result = unsafeItem.MarkAsSafe("Container stopped");
        var updatedItem = result.Value;
        
        result.IsSuccess.Should().BeTrue();
        updatedItem.IsSafeToDelete.Should().BeTrue();
        updatedItem.Reason.Should().Be("Container stopped");
    }

    [Fact]
    public void PreservePathWhenMarkingAsSafe()
    {
        var unsafeItem = new CleanableItemBuilder()
            .ForModule(CleanupModuleName.Docker)
            .Unsafe()
            .Build();
        var expectedPath = unsafeItem.Path;

        var result = unsafeItem.MarkAsSafe("Container stopped");

        result.Value.Path.Should().Be(expectedPath);
    }

    [Fact]
    public void PreserveSizeWhenMarkingAsUnsafe()
    {
        var safeItem = new CleanableItemBuilder()
            .ForModule(CleanupModuleName.Docker)
            .Safe()
            .Build();
        var expectedSize = safeItem.Size;

        var result = safeItem.MarkAsUnsafe("Found in active container");

        result.Value.Size.Should().Be(expectedSize);
    }

    [Fact]
    public void PreserveModuleTypeWhenChangingSafety()
    {
        var safeItem = new CleanableItemBuilder()
            .ForModule(CleanupModuleName.Projects)
            .Safe()
            .WithReason("Project dependencies")
            .Build();

        var result = safeItem.MarkAsUnsafe("Found in active project");

        result.Value.ModuleType.Should().Be(CleanupModuleName.Projects);
    }
}

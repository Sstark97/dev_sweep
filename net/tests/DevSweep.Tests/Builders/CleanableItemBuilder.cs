using DevSweep.Domain.Entities;
using DevSweep.Domain.Enums;
using DevSweep.Domain.ValueObjects;

namespace DevSweep.Tests.Builders;

internal sealed class CleanableItemBuilder
{
    private CleanupModuleName module = CleanupModuleName.Docker;
    private bool safe = true;
    private string reason = "Test cleanup item";
    private long sizeInBytes = 1024;
    private string path = "/any/test/path";

    internal CleanableItemBuilder ForModule(CleanupModuleName module)
    {
        this.module = module;
        return this;
    }

    internal CleanableItemBuilder Safe()
    {
        safe = true;
        reason = "Safe for deletion";
        return this;
    }

    internal CleanableItemBuilder Unsafe()
    {
        safe = false;
        reason = "Currently in use";
        return this;
    }

    internal CleanableItemBuilder WithReason(string reason)
    {
        this.reason = reason;
        return this;
    }

    internal CleanableItemBuilder Small()
    {
        sizeInBytes = 1024;
        return this;
    }

    internal CleanableItemBuilder Large()
    {
        sizeInBytes = 2048;
        return this;
    }

    internal CleanableItemBuilder WithPath(string path)
    {
        this.path = path;
        return this;
    }

    internal CleanableItemBuilder WithSizeInBytes(long bytes)
    {
        sizeInBytes = bytes;
        return this;
    }

    internal CleanableItem Build()
    {
        var filePath = FilePath.Create(path).Value;
        var fileSize = FileSize.Create(sizeInBytes).Value;

        return safe
            ? CleanableItem.CreateSafe(filePath, fileSize, module, reason)
            : CleanableItem.CreateUnsafe(filePath, fileSize, module, reason);
    }
}

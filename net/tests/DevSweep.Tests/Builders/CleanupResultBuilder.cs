using DevSweep.Domain.ValueObjects;

namespace DevSweep.Tests.Builders;

internal sealed class CleanupResultBuilder
{
    private int filesDeleted = 1;
    private long bytesFreed = 1024;

    internal CleanupResultBuilder Empty()
    {
        filesDeleted = 0;
        bytesFreed = 0;
        return this;
    }

    internal CleanupResultBuilder WithFilesDeleted(int count)
    {
        filesDeleted = count;
        return this;
    }

    internal CleanupResultBuilder WithBytesFreed(long bytes)
    {
        bytesFreed = bytes;
        return this;
    }

    internal CleanupResult Build()
    {
        var size = FileSize.Create(bytesFreed).Value;
        return CleanupResult.Create(filesDeleted, size).Value;
    }
}

using DevSweep.Application.Ports.Driven;
using DevSweep.Domain.Common;
using DevSweep.Domain.Errors;

namespace DevSweep.Application.Modules;

public sealed record CleanupContext
{
    private CleanupContext(
        IFileSystem fileSystem,
        IProcessManager processManager,
        ICommandRunner commandRunner,
        IEnvironmentProvider environmentProvider,
        IUserInteraction userInteraction,
        IOutputFormatter outputFormatter)
    {
        FileSystem = fileSystem;
        ProcessManager = processManager;
        CommandRunner = commandRunner;
        EnvironmentProvider = environmentProvider;
        UserInteraction = userInteraction;
        OutputFormatter = outputFormatter;
    }

    public static Result<CleanupContext, DomainError> Create(
        IFileSystem? fileSystem,
        IProcessManager? processManager,
        ICommandRunner? commandRunner,
        IEnvironmentProvider? environmentProvider,
        IUserInteraction? userInteraction,
        IOutputFormatter? outputFormatter)
    {
        if (fileSystem is null)
            return Result<CleanupContext, DomainError>.Failure(
                DomainError.Validation("fileSystem is required"));

        if (processManager is null)
            return Result<CleanupContext, DomainError>.Failure(
                DomainError.Validation("processManager is required"));

        if (commandRunner is null)
            return Result<CleanupContext, DomainError>.Failure(
                DomainError.Validation("commandRunner is required"));

        if (environmentProvider is null)
            return Result<CleanupContext, DomainError>.Failure(
                DomainError.Validation("environmentProvider is required"));

        if (userInteraction is null)
            return Result<CleanupContext, DomainError>.Failure(
                DomainError.Validation("userInteraction is required"));

        if (outputFormatter is null)
            return Result<CleanupContext, DomainError>.Failure(
                DomainError.Validation("outputFormatter is required"));

        return Result<CleanupContext, DomainError>.Success(new CleanupContext(
            fileSystem, processManager, commandRunner,
            environmentProvider, userInteraction, outputFormatter));
    }

    public IFileSystem FileSystem { get; }
    public IProcessManager ProcessManager { get; }
    public ICommandRunner CommandRunner { get; }
    public IEnvironmentProvider EnvironmentProvider { get; }
    public IUserInteraction UserInteraction { get; }
    public IOutputFormatter OutputFormatter { get; }
}

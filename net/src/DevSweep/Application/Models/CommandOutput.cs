using DevSweep.Domain.Common;
using DevSweep.Domain.Errors;

namespace DevSweep.Application.Models;

public readonly record struct CommandOutput
{
    private readonly int exitCode;
    private readonly string standardOutput;
    private readonly string standardError;

    private CommandOutput(int exitCode, string standardOutput, string standardError)
    {
        this.exitCode = exitCode;
        this.standardOutput = standardOutput;
        this.standardError = standardError;
    }

    public static Result<CommandOutput, DomainError> Create(int exitCode, string standardOutput, string standardError)
    {
        if (standardOutput is null)
            return Result<CommandOutput, DomainError>.Failure(
                DomainError.Validation("Standard output cannot be null"));

        if (standardError is null)
            return Result<CommandOutput, DomainError>.Failure(
                DomainError.Validation("Standard error cannot be null"));

        return Result<CommandOutput, DomainError>.Success(
            new CommandOutput(exitCode, standardOutput, standardError));
    }

    public static Result<CommandOutput, DomainError> CreateFailed(string errorMessage)
    {
        if (string.IsNullOrEmpty(errorMessage))
            return Result<CommandOutput, DomainError>.Failure(
                DomainError.Validation("Error message cannot be null or empty"));

        return Result<CommandOutput, DomainError>.Success(
            new CommandOutput(-1, string.Empty, errorMessage));
    }

    public int ExitCode() => exitCode;
    public string StandardOutput() => standardOutput;
    public string StandardError() => standardError;
    public bool IsSuccessful() => exitCode == 0;
    public bool HasOutput() => !string.IsNullOrEmpty(standardOutput);
}

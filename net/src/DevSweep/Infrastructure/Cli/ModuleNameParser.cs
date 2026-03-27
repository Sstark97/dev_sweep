using System.Collections.Frozen;
using DevSweep.Application.Ports.Driving;
using DevSweep.Domain.Common;
using DevSweep.Domain.Enums;
using DevSweep.Domain.Errors;

namespace DevSweep.Infrastructure.Cli;

internal static class ModuleNameParser
{
    private static readonly FrozenDictionary<string, CleanupModuleName?> knownNames =
        new Dictionary<string, CleanupModuleName?>
        {
            ["JETBRAINS"] = CleanupModuleName.JetBrains,
            ["DOCKER"] = CleanupModuleName.Docker,
            ["HOMEBREW"] = CleanupModuleName.Homebrew,
            ["DEVTOOLS"] = CleanupModuleName.DevTools,
            ["PROJECTS"] = CleanupModuleName.Projects,
            ["SYSTEM"] = CleanupModuleName.System,
            ["NODEJS"] = CleanupModuleName.NodeJs
        }.ToFrozenDictionary();

    private static readonly IReadOnlyList<string> displayNames =
        ["jetbrains", "docker", "homebrew", "devtools", "projects", "system", "nodejs"];

    private static readonly string validNamesDisplay = string.Join(", ", displayNames);

    public static Result<CleanupModuleName, DomainError> Parse(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return Result<CleanupModuleName, DomainError>.Failure(
                DomainError.Validation($"Module name cannot be empty. Valid names: {validNamesDisplay}"));

        var normalized = name.ToUpperInvariant();
        var found = knownNames.GetValueOrDefault(normalized);

        if (found is null)
            return Result<CleanupModuleName, DomainError>.Failure(
                DomainError.Validation($"Unknown module '{name}'. Valid names: {validNamesDisplay}"));

        return Result<CleanupModuleName, DomainError>.Success(found.Value);
    }

    public static Result<IReadOnlyList<CleanupModuleName>, DomainError> ParseMany(IReadOnlyList<string> names)
    {
        var parsed = new List<CleanupModuleName>(names.Count);

        foreach (var name in names)
        {
            var result = Parse(name);
            if (result.IsFailure)
                return Result<IReadOnlyList<CleanupModuleName>, DomainError>.Failure(result.Error);

            parsed.Add(result.Value);
        }

        return Result<IReadOnlyList<CleanupModuleName>, DomainError>.Success(parsed);
    }

    public static Result<IReadOnlyList<CleanupModuleName>, DomainError> Resolve(
        IReadOnlyList<string> modules,
        bool all,
        IAvailableModulesUseCase availableModulesUseCase)
    {
        if (all)
        {
            var allModules = availableModulesUseCase.Invoke();
            return Result<IReadOnlyList<CleanupModuleName>, DomainError>.Success(
                [.. allModules.Select(m => m.Name())]);
        }

        if (modules.Count == 0)
            return Result<IReadOnlyList<CleanupModuleName>, DomainError>.Failure(
                DomainError.Validation("Specify at least one module or use --all"));

        return ParseMany(modules);
    }

    public static IReadOnlyList<string> ValidNames() => displayNames;
}

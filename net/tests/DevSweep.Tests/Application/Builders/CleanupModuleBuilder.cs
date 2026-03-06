using DevSweep.Application.Models;
using DevSweep.Application.Modules;
using DevSweep.Domain.Common;
using DevSweep.Domain.Entities;
using DevSweep.Domain.Enums;
using DevSweep.Domain.Errors;
using DevSweep.Domain.ValueObjects;
using NSubstitute;

namespace DevSweep.Tests.Application.Builders;

internal sealed class CleanupModuleBuilder
{
    private readonly ICleanupModule module;
    private CleanupModuleName name = CleanupModuleName.Docker;
    private string description = "Test module";
    private bool destructive;
    private OperatingSystemType? availableOn;
    private OperatingSystemType? notAvailableOn;
    private ModuleAnalysis? analysis;
    private DomainError? analysisError;
    private CleanupResult? cleanResult;
    private DomainError? cleanError;

    internal CleanupModuleBuilder(ICleanupModule module)
    {
        this.module = module;
    }

    internal CleanupModuleBuilder ForModule(CleanupModuleName moduleName)
    {
        name = moduleName;
        return this;
    }

    internal CleanupModuleBuilder WithDescription(string moduleDescription)
    {
        description = moduleDescription;
        return this;
    }

    internal CleanupModuleBuilder Destructive()
    {
        destructive = true;
        return this;
    }

    internal CleanupModuleBuilder NonDestructive()
    {
        destructive = false;
        return this;
    }

    internal CleanupModuleBuilder AvailableOn(OperatingSystemType os)
    {
        availableOn = os;
        return this;
    }

    internal CleanupModuleBuilder NotAvailableOn(OperatingSystemType os)
    {
        notAvailableOn = os;
        return this;
    }

    internal CleanupModuleBuilder WithAnalysis(ModuleAnalysis moduleAnalysis)
    {
        analysis = moduleAnalysis;
        return this;
    }

    internal CleanupModuleBuilder WithAnalysisError(DomainError error)
    {
        analysisError = error;
        return this;
    }

    internal CleanupModuleBuilder WithCleanResult(CleanupResult result)
    {
        cleanResult = result;
        return this;
    }

    internal CleanupModuleBuilder WithCleanError(DomainError error)
    {
        cleanError = error;
        return this;
    }

    internal ICleanupModule Configure()
    {
        module.Name.Returns(name);
        module.Description.Returns(description);
        module.IsDestructive.Returns(destructive);

        if (availableOn.HasValue)
        {
            module.IsAvailableOnPlatform(Arg.Any<OperatingSystemType>()).Returns(false);
            module.IsAvailableOnPlatform(availableOn.Value).Returns(true);
        }
        else if (notAvailableOn.HasValue)
        {
            module.IsAvailableOnPlatform(Arg.Any<OperatingSystemType>()).Returns(true);
            module.IsAvailableOnPlatform(notAvailableOn.Value).Returns(false);
        }
        else
        {
            module.IsAvailableOnPlatform(Arg.Any<OperatingSystemType>()).Returns(true);
        }

        if (analysis is not null)
            module.AnalyzeAsync(Arg.Any<CancellationToken>())
                .Returns(Result<ModuleAnalysis, DomainError>.Success(analysis));

        if (analysisError.HasValue)
            module.AnalyzeAsync(Arg.Any<CancellationToken>())
                .Returns(Result<ModuleAnalysis, DomainError>.Failure(analysisError.Value));

        if (cleanResult.HasValue)
            module.CleanAsync(Arg.Any<IReadOnlyList<CleanableItem>>(), Arg.Any<CancellationToken>())
                .Returns(Result<CleanupResult, DomainError>.Success(cleanResult.Value));

        if (cleanError.HasValue)
            module.CleanAsync(Arg.Any<IReadOnlyList<CleanableItem>>(), Arg.Any<CancellationToken>())
                .Returns(Result<CleanupResult, DomainError>.Failure(cleanError.Value));

        return module;
    }
}

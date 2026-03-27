using AwesomeAssertions;
using DevSweep.Domain.Enums;
using DevSweep.Infrastructure.Cli;

namespace DevSweep.Tests.Infrastructure.Cli;

internal sealed class ModuleNameParserShould
{
    [Test]
    public void ParseValidModuleNameCaseInsensitively()
    {
        var lowerResult = ModuleNameParser.Parse("jetbrains");
        var mixedResult = ModuleNameParser.Parse("JetBrains");
        var upperResult = ModuleNameParser.Parse("JETBRAINS");

        lowerResult.IsSuccess.Should().BeTrue();
        mixedResult.IsSuccess.Should().BeTrue();
        upperResult.IsSuccess.Should().BeTrue();
    }

    [Test]
    public void ParseAllKnownModuleNames()
    {
        var jetbrains = ModuleNameParser.Parse("jetbrains");
        var docker = ModuleNameParser.Parse("docker");
        var homebrew = ModuleNameParser.Parse("homebrew");
        var devtools = ModuleNameParser.Parse("devtools");
        var projects = ModuleNameParser.Parse("projects");
        var system = ModuleNameParser.Parse("system");
        var nodejs = ModuleNameParser.Parse("nodejs");

        jetbrains.Value.Should().Be(CleanupModuleName.JetBrains);
        docker.Value.Should().Be(CleanupModuleName.Docker);
        homebrew.Value.Should().Be(CleanupModuleName.Homebrew);
        devtools.Value.Should().Be(CleanupModuleName.DevTools);
        projects.Value.Should().Be(CleanupModuleName.Projects);
        system.Value.Should().Be(CleanupModuleName.System);
        nodejs.Value.Should().Be(CleanupModuleName.NodeJs);
    }

    [Test]
    public void FailWhenModuleNameIsUnknown()
    {
        var result = ModuleNameParser.Parse("invalid");

        result.IsFailure.Should().BeTrue();
        result.Error.IsValidationError().Should().BeTrue();
        result.Error.MessageContains("invalid").Should().BeTrue();
    }

    [Test]
    public void FailWhenModuleNameIsEmpty()
    {
        var result = ModuleNameParser.Parse("");

        result.IsFailure.Should().BeTrue();
        result.Error.IsValidationError().Should().BeTrue();
    }

    [Test]
    public void ParseManyValidModuleNames()
    {
        var result = ModuleNameParser.ParseMany(["jetbrains", "docker"]);

        var modules = result.Value;

        result.IsSuccess.Should().BeTrue();
        modules.Count.Should().Be(2);
        modules.Should().Contain(CleanupModuleName.JetBrains);
        modules.Should().Contain(CleanupModuleName.Docker);
    }

    [Test]
    public void FailOnFirstInvalidNameInList()
    {
        var result = ModuleNameParser.ParseMany(["jetbrains", "invalid"]);

        result.IsFailure.Should().BeTrue();
        result.Error.MessageContains("invalid").Should().BeTrue();
    }

    [Test]
    public void ParseManyWithEmptyListSuccessfully()
    {
        var result = ModuleNameParser.ParseMany([]);

        var modules = result.Value;

        result.IsSuccess.Should().BeTrue();
        modules.Count.Should().Be(0);
    }
}

using AwesomeAssertions;
using DevSweep.Application.Models;
using DevSweep.Domain.Enums;
using DevSweep.Infrastructure.Cli.Interaction;

namespace DevSweep.Tests.Infrastructure.Cli.Interaction;

internal sealed class SelectAllModulesShould
{
    [Test]
    public async Task SelectAllModulesFromNonEmptyList()
    {
        var dockerDescriptor = ModuleDescriptor.Create(CleanupModuleName.Docker, "Docker cache", false).Value;
        var homebrewDescriptor = ModuleDescriptor.Create(CleanupModuleName.Homebrew, "Homebrew cache", false).Value;
        var selector = new SelectAllModules();

        var selected = await selector.SelectModulesAsync([dockerDescriptor, homebrewDescriptor], CancellationToken.None);

        selected.Count.Should().Be(2);
        selected.Should().Contain(CleanupModuleName.Docker);
        selected.Should().Contain(CleanupModuleName.Homebrew);
    }

    [Test]
    public async Task SelectNoModulesWhenNoneAvailable()
    {
        var selector = new SelectAllModules();

        var selected = await selector.SelectModulesAsync([], CancellationToken.None);

        selected.Should().BeEmpty();
    }

    [Test]
    public async Task SelectAllAvailableModulesRegardlessOfCancellation()
    {
        var dockerDescriptor = ModuleDescriptor.Create(CleanupModuleName.Docker, "Docker cache", false).Value;
        var selector = new SelectAllModules();
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        var selected = await selector.SelectModulesAsync([dockerDescriptor], cts.Token);

        selected.Count.Should().Be(1);
        selected.Should().Contain(CleanupModuleName.Docker);
    }
}

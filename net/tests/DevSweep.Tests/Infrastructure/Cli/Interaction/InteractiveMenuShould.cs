using AwesomeAssertions;
using DevSweep.Application.Models;
using DevSweep.Domain.Enums;
using DevSweep.Infrastructure.Cli.Interaction;
using Spectre.Console;
using Spectre.Console.Testing;

namespace DevSweep.Tests.Infrastructure.Cli.Interaction;

internal sealed class InteractiveMenuShould
{
    [Test]
    public async Task SelectNoModulesWhenCancellationRequested()
    {
        using var console = new TestConsole();
        var dockerDescriptor = ModuleDescriptor.Create(CleanupModuleName.Docker, "Docker cache", false).Value;
        var menu = new InteractiveMenu(console);
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        var selected = await menu.SelectModulesAsync([dockerDescriptor], cts.Token);

        selected.Should().BeEmpty();
    }

    [Test]
    public async Task SelectModulesMatchingUserChoices()
    {
        using var console = new TestConsole();
        console.Interactive();
        console.Input.PushKey(ConsoleKey.Enter);
        var dockerDescriptor = ModuleDescriptor.Create(CleanupModuleName.Docker, "Docker cache", false).Value;
        var menu = new InteractiveMenu(console);

        var selected = await menu.SelectModulesAsync([dockerDescriptor], CancellationToken.None);

        selected.Count.Should().Be(1);
        selected.Should().Contain(CleanupModuleName.Docker);
    }

    [Test]
    public async Task SelectNoModulesWhenUserSelectsNone()
    {
        using var console = new TestConsole();
        console.Interactive();
        console.Input.PushKey(ConsoleKey.Spacebar);
        console.Input.PushKey(ConsoleKey.Enter);
        var dockerDescriptor = ModuleDescriptor.Create(CleanupModuleName.Docker, "Docker cache", false).Value;
        var menu = new InteractiveMenu(console);

        var selected = await menu.SelectModulesAsync([dockerDescriptor], CancellationToken.None);

        selected.Should().BeEmpty();
    }

    [Test]
    public async Task SelectSubsetWhenUserDeselectsSomeModules()
    {
        using var console = new TestConsole();
        console.Interactive();
        console.Input.PushKey(ConsoleKey.Spacebar);
        console.Input.PushKey(ConsoleKey.Enter);
        var dockerDescriptor = ModuleDescriptor.Create(CleanupModuleName.Docker, "Docker cache", false).Value;
        var homebrewDescriptor = ModuleDescriptor.Create(CleanupModuleName.Homebrew, "Homebrew cache", false).Value;
        var jetbrainsDescriptor = ModuleDescriptor.Create(CleanupModuleName.JetBrains, "JetBrains cache", false).Value;
        var menu = new InteractiveMenu(console);

        var selected = await menu.SelectModulesAsync([dockerDescriptor, homebrewDescriptor, jetbrainsDescriptor], CancellationToken.None);

        selected.Count.Should().Be(2);
        selected.Should().NotContain(CleanupModuleName.Docker);
        selected.Should().Contain(CleanupModuleName.Homebrew);
        selected.Should().Contain(CleanupModuleName.JetBrains);
    }
}

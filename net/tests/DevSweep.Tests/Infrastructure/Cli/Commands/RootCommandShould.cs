using AwesomeAssertions;
using DevSweep.Application.Ports.Driven;
using DevSweep.Infrastructure.Cli.Commands;
using NSubstitute;

namespace DevSweep.Tests.Infrastructure.Cli.Commands;

internal sealed class RootCommandShould
{
    private readonly IOutputFormatter outputFormatter = Substitute.For<IOutputFormatter>();

    [Test]
    public async Task DisplayHelpMessageWhenInvokedDirectly()
    {
        var command = new RootCommand(outputFormatter);

        await command.RunAsync();

        outputFormatter.Received().Info(Arg.Any<string>());
    }

    [Test]
    public async Task ReturnSuccessExitCode()
    {
        var command = new RootCommand(outputFormatter);

        var exitCode = await command.RunAsync();

        exitCode.Should().Be(0);
    }
}

using AwesomeAssertions;
using DevSweep.Application.Ports.Driven;
using DevSweep.Infrastructure.Cli.Commands;
using NSubstitute;

namespace DevSweep.Tests.Infrastructure.Cli.Commands;

internal sealed class VersionCommandShould
{
    private readonly IOutputFormatter outputFormatter = Substitute.For<IOutputFormatter>();

    [Test]
    public async Task DisplayVersionInformation()
    {
        var command = new VersionCommand(outputFormatter);

        await command.RunAsync();

        outputFormatter.Received().Info(Arg.Is<string>(m => m.Contains("DevSweep")));
    }

    [Test]
    public async Task ReturnSuccessExitCode()
    {
        var command = new VersionCommand(outputFormatter);

        var exitCode = await command.RunAsync();

        exitCode.Should().Be(0);
    }
}

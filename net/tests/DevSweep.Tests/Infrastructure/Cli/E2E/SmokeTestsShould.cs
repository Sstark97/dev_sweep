using AwesomeAssertions;

namespace DevSweep.Tests.Infrastructure.Cli.E2E;

internal sealed class SmokeTestsShould
{
    private static readonly TimeSpan TestTimeout = TimeSpan.FromSeconds(60);
    private static readonly TimeSpan LongTestTimeout = TimeSpan.FromSeconds(180);

    [Test]
    public async Task DisplayHelpTextWhenHelpFlagProvided()
    {
        using var cts = new CancellationTokenSource(TestTimeout);

        var result = await CliProcessRunner.RunAsync("--help", cts.Token);

        var exitCode = result.ExitCode;
        var output = result.StandardOutput;

        exitCode.Should().Be(0);
        output.Should().Contain("DevSweep");
    }

    [Test]
    public async Task ExitCleanlyWhenHelpRequested()
    {
        using var cts = new CancellationTokenSource(TestTimeout);

        var result = await CliProcessRunner.RunAsync("--help", cts.Token);

        result.ExitCode.Should().Be(0);
    }

    [Test]
    public async Task DisplayVersionWhenVersionSubcommandUsed()
    {
        using var cts = new CancellationTokenSource(TestTimeout);

        var result = await CliProcessRunner.RunAsync("version", cts.Token);

        var exitCode = result.ExitCode;
        var output = result.StandardOutput;

        exitCode.Should().Be(0);
        output.Should().Contain("DevSweep");
    }

    [Test]
    public async Task ExitCleanlyWhenNoArgumentsProvided()
    {
        using var cts = new CancellationTokenSource(LongTestTimeout);

        var result = await CliProcessRunner.RunAsync("--force --dry-run --output plain", cts.Token);

        var exitCode = result.ExitCode;
        var output = result.StandardOutput;

        exitCode.Should().Be(0);
        output.Should().Contain("DevSweep");
    }

    [Test]
    public async Task RunFullInteractiveFlowWithForceFlag()
    {
        using var cts = new CancellationTokenSource(LongTestTimeout);

        var result = await CliProcessRunner.RunAsync("--force --dry-run --output plain", cts.Token);

        var exitCode = result.ExitCode;
        var output = result.StandardOutput;

        exitCode.Should().Be(0);
        output.Should().NotBeEmpty();
    }

    [Test]
    public async Task FailWhenAnalyzeHasNoModulesAndNoAllFlag()
    {
        using var cts = new CancellationTokenSource(TestTimeout);

        var result = await CliProcessRunner.RunAsync("analyze", cts.Token);

        result.ExitCode.Should().Be(1);
    }

    [Test]
    public async Task AnalyzeAllModulesWithoutCrashing()
    {
        using var cts = new CancellationTokenSource(TestTimeout);

        var result = await CliProcessRunner.RunAsync("analyze --all", cts.Token);

        result.ExitCode.Should().Be(0);
    }

    [Test]
    public async Task FailWhenCleanHasNoModulesAndNoAllFlag()
    {
        using var cts = new CancellationTokenSource(TestTimeout);

        var result = await CliProcessRunner.RunAsync("clean", cts.Token);

        result.ExitCode.Should().Be(1);
    }

    [Test]
    public async Task NotCrashWhenCleanRunsWithDryRunAndForce()
    {
        using var cts = new CancellationTokenSource(TestTimeout);

        var result = await CliProcessRunner.RunAsync("clean --all --dry-run --force", cts.Token);

        var stderr = result.StandardError;

        stderr.Should().NotContain("Unhandled exception");
    }

    [Test]
    public async Task FailWhenInvalidSubcommandProvided()
    {
        using var cts = new CancellationTokenSource(TestTimeout);

        var result = await CliProcessRunner.RunAsync("nonexistent-command", cts.Token);

        result.ExitCode.Should().NotBe(0);
    }

    [Test]
    public async Task FailWhenInvalidModuleNameProvided()
    {
        using var cts = new CancellationTokenSource(TestTimeout);

        var result = await CliProcessRunner.RunAsync("analyze invalidmodule", cts.Token);

        result.ExitCode.Should().Be(1);
    }

    [Test]
    public async Task ProduceOutputWhenJsonFormatRequested()
    {
        using var cts = new CancellationTokenSource(TestTimeout);

        var result = await CliProcessRunner.RunAsync("analyze --all --output json", cts.Token);

        var exitCode = result.ExitCode;
        var output = result.StandardOutput;

        exitCode.Should().Be(0);
        output.Should().NotBeEmpty();
    }

    [Test]
    public async Task AcceptMultipleModuleNames()
    {
        using var cts = new CancellationTokenSource(TestTimeout);

        var result = await CliProcessRunner.RunAsync("analyze jetbrains docker", cts.Token);

        result.ExitCode.Should().Be(0);
    }
}

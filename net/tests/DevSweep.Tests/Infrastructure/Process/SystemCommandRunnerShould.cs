using AwesomeAssertions;
using DevSweep.Infrastructure.Process;

namespace DevSweep.Tests.Infrastructure.Process;

internal sealed class SystemCommandRunnerShould : IDisposable
{
    private readonly SystemCommandRunner commandRunner = new();
    private readonly List<IDisposable> disposables = [];

    [Test]
    public void DetectAvailableCommand()
    {
        var isAvailable = commandRunner.IsCommandAvailable("dotnet");

        isAvailable.Should().BeTrue();
    }

    [Test]
    public void NotDetectUnavailableCommand()
    {
        var isAvailable = commandRunner.IsCommandAvailable("devsweep-nonexistent-command-xyz");

        isAvailable.Should().BeFalse();
    }

    [Test]
    public async Task ProduceOutputForValidCommand()
    {
        var result = await commandRunner.RunAsync("dotnet", "--version", CancellationToken.None);

        var output = result.Value;
        result.IsSuccess.Should().BeTrue();
        output.IsSuccessful().Should().BeTrue();
        output.StandardOutput().Trim().Should().NotBeEmpty();
        output.ExitCode().Should().Be(0);
    }

    [Test]
    public async Task FailForInvalidSubcommand()
    {
        var result = await commandRunner.RunAsync("dotnet", "nonexistent-subcommand-xyz", CancellationToken.None);

        var output = result.Value;
        result.IsSuccess.Should().BeTrue();
        output.IsSuccessful().Should().BeFalse();
        output.ExitCode().Should().NotBe(0);
    }

    [Test]
    public async Task RunCommandWithMultipleArguments()
    {
        var result = await commandRunner.RunAsync("dotnet", "--info", CancellationToken.None);

        var output = result.Value;
        result.IsSuccess.Should().BeTrue();
        output.IsSuccessful().Should().BeTrue();
        output.HasOutput().Should().BeTrue();
    }

    [Test]
    public async Task FailWhenCancellationIsRequested()
    {
        var cancellationSource = Track(new CancellationTokenSource());
        await cancellationSource.CancelAsync();

        var result = await commandRunner.RunAsync("dotnet", "--version", cancellationSource.Token);

        result.IsFailure.Should().BeTrue();
        result.Error.IsInvalidOperationError().Should().BeTrue();
    }

    [Test]
    public async Task FailGracefullyForNonExistentCommand()
    {
        var result = await commandRunner.RunAsync(
            "devsweep-nonexistent-command-xyz", "", CancellationToken.None);

        var output = result.Value;
        result.IsSuccess.Should().BeTrue();
        output.IsSuccessful().Should().BeFalse();
        output.StandardError().Should().NotBeEmpty();
    }

    private T Track<T>(T disposable) where T : IDisposable
    {
        disposables.Add(disposable);
        return disposable;
    }

    public void Dispose()
    {
        foreach (var disposable in disposables)
            disposable.Dispose();
    }
}

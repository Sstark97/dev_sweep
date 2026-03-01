using DevSweep.Application.Models;

namespace DevSweep.Tests.Application.Models;

public class CommandOutputShould
{
    [Fact]
    public void SucceedWithValidParameters()
    {
        var result = CommandOutput.Create(0, "output text", string.Empty);

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public void FailWhenStandardOutputIsNull()
    {
        var result = CommandOutput.Create(0, null!, string.Empty);

        result.IsFailure.Should().BeTrue();
        result.Error.MessageContains("standard output").Should().BeTrue();
    }

    [Fact]
    public void FailWhenStandardErrorIsNull()
    {
        var result = CommandOutput.Create(0, string.Empty, null!);

        result.IsFailure.Should().BeTrue();
        result.Error.MessageContains("standard error").Should().BeTrue();
    }

    [Fact]
    public void ReportSuccessfulWhenExitCodeIsZero()
    {
        var outputResult = CommandOutput.Create(0, "done", string.Empty);
        var output = outputResult.Value;

        outputResult.IsSuccess.Should().BeTrue();
        output.IsSuccessful().Should().BeTrue();
    }

    [Fact]
    public void ReportUnsuccessfulWhenExitCodeIsNonZero()
    {
        var outputResult = CommandOutput.Create(1, string.Empty, "error occurred");
        var output = outputResult.Value;

        outputResult.IsSuccess.Should().BeTrue();
        output.IsSuccessful().Should().BeFalse();
    }

    [Fact]
    public void ReportHasOutputWhenStandardOutputIsNotEmpty()
    {
        var outputResult = CommandOutput.Create(0, "some output", string.Empty);
        var output = outputResult.Value;

        outputResult.IsSuccess.Should().BeTrue();
        output.HasOutput().Should().BeTrue();
    }

    [Fact]
    public void ReportNoOutputWhenStandardOutputIsEmpty()
    {
        var outputResult = CommandOutput.Create(0, string.Empty, string.Empty);
        var output = outputResult.Value;

        outputResult.IsSuccess.Should().BeTrue();
        output.HasOutput().Should().BeFalse();
    }

    [Fact]
    public void CreateFailedCommandOutput()
    {
        var result = CommandOutput.CreateFailed("command not found");
        var failedOutput = result.Value;

        result.IsSuccess.Should().BeTrue();
        failedOutput.ExitCode().Should().Be(-1);
        failedOutput.IsSuccessful().Should().BeFalse();
        failedOutput.StandardError().Should().Be("command not found");
    }
}

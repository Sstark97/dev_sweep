using DevSweep.Domain.Common;

namespace DevSweep.Tests.Domain.Common;

public class ResultMatchTests
{
    [Fact]
    public void ExecutesSuccessFunctionWhenResultIsSuccess()
    {
        var result = Result<int, string>.Success(42);
        var wasCalled = false;

        result.Match(
            onSuccess: value => { wasCalled = true; return value; },
            onFailure: error => 0);

        wasCalled.Should().BeTrue();
    }

    [Fact]
    public void ExecutesFailureFunctionWhenResultIsFailure()
    {
        var result = Result<int, string>.Failure("error");
        var wasCalled = false;

        result.Match(
            onSuccess: value => value,
            onFailure: error => { wasCalled = true; return 0; });

        wasCalled.Should().BeTrue();
    }

    [Fact]
    public void ReturnsCorrectValueFromMatchFunction()
    {
        var successResult = Result<int, string>.Success(42);
        var failureResult = Result<int, string>.Failure("error");

        var successMessage = successResult.Match(
            onSuccess: value => $"Success: {value}",
            onFailure: error => $"Failure: {error}");

        var failureMessage = failureResult.Match(
            onSuccess: value => $"Success: {value}",
            onFailure: error => $"Failure: {error}");

        successMessage.Should().Be("Success: 42");
        failureMessage.Should().Be("Failure: error");
    }
}

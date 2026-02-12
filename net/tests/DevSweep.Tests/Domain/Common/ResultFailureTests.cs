using DevSweep.Domain.Common;

namespace DevSweep.Tests.Domain.Common;

public class ResultFailureTests
{
    [Fact]
    public void CreatesFailureResultWithError()
    {
        var result = Result<int, string>.Failure("error");

        result.Should().NotBeNull();
    }

    [Fact]
    public void IsFailureReturnsTrueForFailureResult()
    {
        var result = Result<int, string>.Failure("error");

        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public void IsSuccessReturnsFalseForFailureResult()
    {
        var result = Result<int, string>.Failure("error");

        result.IsSuccess.Should().BeFalse();
    }

    [Fact]
    public void ErrorReturnsCorrectErrorForFailureResult()
    {
        var result = Result<int, string>.Failure("error message");

        result.Error.Should().Be("error message");
    }

    [Fact]
    public void ValueThrowsExceptionForFailureResult()
    {
        var result = Result<int, string>.Failure("error");

        var act = () => { var value = result.Value; };

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("Cannot access Value of failed result");
    }
}

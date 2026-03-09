using AwesomeAssertions;
using DevSweep.Domain.Common;

namespace DevSweep.Tests.Domain.Common;

internal sealed class ResultFailureShould
{
    [Test]
    public void IsFailureReturnsTrueForFailureResult()
    {
        var result = Result<int, string>.Failure("error");

        result.IsFailure.Should().BeTrue();
    }

    [Test]
    public void IsSuccessReturnsFalseForFailureResult()
    {
        var result = Result<int, string>.Failure("error");

        result.IsSuccess.Should().BeFalse();
    }

    [Test]
    public void ErrorReturnsCorrectErrorForFailureResult()
    {
        var result = Result<int, string>.Failure("error message");

        result.Error.Should().Be("error message");
    }

    [Test]
    public void ValueThrowsExceptionForFailureResult()
    {
        var result = Result<int, string>.Failure("error");

        var act = () => { var value = result.Value; };

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("Cannot access Value of failed result");
    }
}

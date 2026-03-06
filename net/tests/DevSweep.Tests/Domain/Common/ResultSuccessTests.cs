using AwesomeAssertions;
using DevSweep.Domain.Common;

namespace DevSweep.Tests.Domain.Common;

internal sealed class ResultSuccessTests
{
    [Test]
    public void IsSuccessReturnsTrueForSuccessResult()
    {
        var result = Result<int, string>.Success(42);

        result.IsSuccess.Should().BeTrue();
    }

    [Test]
    public void IsFailureReturnsFalseForSuccessResult()
    {
        var result = Result<int, string>.Success(42);

        result.IsFailure.Should().BeFalse();
    }

    [Test]
    public void ValueReturnsCorrectValueForSuccessResult()
    {
        var result = Result<int, string>.Success(42);

        result.Value.Should().Be(42);
    }

    [Test]
    public void ErrorThrowsExceptionForSuccessResult()
    {
        var result = Result<int, string>.Success(42);

        Action act = () => { var error = result.Error; };

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("Cannot access Error of successful result");
    }
}

using DevSweep.Domain.Common;

namespace DevSweep.Tests.Domain.Common;

public class ResultSuccessTests
{
    [Fact]
    public void IsSuccessReturnsTrueForSuccessResult()
    {
        var result = Result<int, string>.Success(42);

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public void IsFailureReturnsFalseForSuccessResult()
    {
        var result = Result<int, string>.Success(42);

        result.IsFailure.Should().BeFalse();
    }

    [Fact]
    public void ValueReturnsCorrectValueForSuccessResult()
    {
        var result = Result<int, string>.Success(42);

        result.Value.Should().Be(42);
    }

    [Fact]
    public void ErrorThrowsExceptionForSuccessResult()
    {
        var result = Result<int, string>.Success(42);

        Action act = () => { var error = result.Error; };

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("Cannot access Error of successful result");
    }
}

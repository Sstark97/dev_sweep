using DevSweep.Domain.Common;

namespace DevSweep.Tests.Domain.Common;

public class ResultLinqQuerySyntaxTests
{
    [Fact]
    public void TransformsValueUsingSelectSyntax()
    {
        var result =
            from x in Result<int, string>.Success(5)
            select x * 2;

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(10);
    }

    [Fact]
    public void ComposesMultipleOperationsUsingQuerySyntax()
    {
        var result =
            from x in Result<int, string>.Success(5)
            from y in Result<int, string>.Success(10)
            select x + y;

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(15);
    }

    [Fact]
    public void PropagatesFirstErrorInQueryChain()
    {
        var result =
            from x in Result<int, string>.Failure("first error")
            from y in Result<int, string>.Success(10)
            select x + y;

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be("first error");
    }

    [Fact]
    public void AllowsComplexRailwayOrientedComposition()
    {
        var result =
            from x in Result<int, string>.Success(5)
            from y in Result<int, string>.Success(10)
            from z in Result<int, string>.Success(3)
            let sum = x + y + z
            select sum * 2;

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(36);
    }
}

using System.Globalization;
using AwesomeAssertions;
using DevSweep.Domain.Common;

namespace DevSweep.Tests.Domain.Common;

internal sealed class ResultBindShould
{
    [Test]
    public void ChainsSuccessfulOperations()
    {
        var result = Result<int, string>.Success(5);

        var bound = result.Bind(x => Result<int, string>.Success(x * 2));

        bound.IsSuccess.Should().BeTrue();
        bound.Value.Should().Be(10);
    }

    [Test]
    public void PropagatesFirstErrorInChain()
    {
        var result = Result<int, string>.Failure("first error");

        var bound = result.Bind(x => Result<int, string>.Success(x * 2));

        bound.IsFailure.Should().BeTrue();
        bound.Error.Should().Be("first error");
    }

    [Test]
    public void AllowsNestedBindOperations()
    {
        var result = Result<int, string>.Success(5);

        var bound = result
            .Bind(x => Result<int, string>.Success(x * 2))
            .Bind(x => Result<int, string>.Success(x + 3))
            .Bind(x => Result<string, string>.Success(x.ToString(CultureInfo.InvariantCulture)));

        bound.IsSuccess.Should().BeTrue();
        bound.Value.Should().Be("13");
    }
}

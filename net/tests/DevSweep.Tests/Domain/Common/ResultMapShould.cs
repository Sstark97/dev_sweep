using System.Globalization;
using AwesomeAssertions;
using DevSweep.Domain.Common;

namespace DevSweep.Tests.Domain.Common;

internal sealed class ResultMapShould
{
    [Test]
    public void TransformsValueWhenResultIsSuccess()
    {
        var result = Result<int, string>.Success(5);

        var mapped = result.Map(x => x * 2);

        mapped.IsSuccess.Should().BeTrue();
        mapped.Value.Should().Be(10);
    }

    [Test]
    public void PropagatesErrorWhenResultIsFailure()
    {
        var result = Result<int, string>.Failure("error");

        var mapped = result.Map(x => x * 2);

        mapped.IsFailure.Should().BeTrue();
        mapped.Error.Should().Be("error");
    }

    [Test]
    public void AllowsChainingMultipleMapOperations()
    {
        var result = Result<int, string>.Success(5);

        var mapped = result
            .Map(x => x * 2)
            .Map(x => x + 3)
            .Map(x => x.ToString(CultureInfo.InvariantCulture));

        mapped.IsSuccess.Should().BeTrue();
        mapped.Value.Should().Be("13");
    }
}

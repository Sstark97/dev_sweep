using AwesomeAssertions;
using DevSweep.Domain.Common;

namespace DevSweep.Tests.Domain.Common;

internal sealed class ResultLinqExtensionsRecoverShould
{
    [Test]
    public void PassThroughSuccessWhenRecoveringWithFallback()
    {
        var result = Result<int, string>.Success(99);

        var recovered = result.Recover(0);

        recovered.IsSuccess.Should().BeTrue();
        recovered.Value.Should().Be(99);
    }

    [Test]
    public void ReturnFallbackWhenFailureRecovering()
    {
        var result = Result<int, string>.Failure("error");

        var recovered = result.Recover(42);

        recovered.IsSuccess.Should().BeTrue();
        recovered.Value.Should().Be(42);
    }

    [Test]
    public void PassThroughSuccessWhenRecoveringWithFactory()
    {
        var result = Result<int, string>.Success(7);

        var recovered = result.Recover(error => -1);

        recovered.IsSuccess.Should().BeTrue();
        recovered.Value.Should().Be(7);
    }

    [Test]
    public void ApplyFactoryToErrorWhenFailureRecovering()
    {
        var result = Result<string, string>.Failure("original error");

        var recovered = result.Recover(error => $"recovered:{error}");

        recovered.IsSuccess.Should().BeTrue();
        recovered.Value.Should().Be("recovered:original error");
    }
}

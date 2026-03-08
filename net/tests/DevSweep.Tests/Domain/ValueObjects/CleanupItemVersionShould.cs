using AwesomeAssertions;
using DevSweep.Domain.ValueObjects;

namespace DevSweep.Tests.Domain.ValueObjects;

internal sealed class CleanupItemVersionShould
{
    [Test]
    public void OrderVersionsWhenSingleSegmentIsProvided()
    {
        var olderResult = CleanupItemVersion.Create("2023");
        var newerResult = CleanupItemVersion.Create("2024");
        var older = olderResult.Value;
        var newer = newerResult.Value;

        olderResult.IsSuccess.Should().BeTrue();
        newerResult.IsSuccess.Should().BeTrue();
        older.Should().BeLessThan(newer);
    }

    [Test]
    public void OrderVersionsWhenTwoSegmentsAreProvided()
    {
        var olderResult = CleanupItemVersion.Create("2024.1");
        var newerResult = CleanupItemVersion.Create("2024.2");
        var older = olderResult.Value;
        var newer = newerResult.Value;

        olderResult.IsSuccess.Should().BeTrue();
        newerResult.IsSuccess.Should().BeTrue();
        older.Should().BeLessThan(newer);
    }

    [Test]
    public void OrderVersionsWhenThreeSegmentsAreProvided()
    {
        var baseResult = CleanupItemVersion.Create("2023.3");
        var patchResult = CleanupItemVersion.Create("2023.3.1");
        var base_ = baseResult.Value;
        var patch = patchResult.Value;

        baseResult.IsSuccess.Should().BeTrue();
        patchResult.IsSuccess.Should().BeTrue();
        base_.Should().BeLessThan(patch);
    }

    [Test]
    public void OrderVersionsWhenFourSegmentsAreProvided()
    {
        var releaseResult = CleanupItemVersion.Create("2023.3.1");
        var buildResult = CleanupItemVersion.Create("2023.3.1.1");
        var release = releaseResult.Value;
        var build = buildResult.Value;

        releaseResult.IsSuccess.Should().BeTrue();
        buildResult.IsSuccess.Should().BeTrue();
        release.Should().BeLessThan(build);
    }

    [Test]
    public void FailCreationWhenVersionIsEmpty()
    {
        var result = CleanupItemVersion.Create(string.Empty);

        result.IsFailure.Should().BeTrue();
        result.Error.IsValidationError().Should().BeTrue();
    }

    [Test]
    public void FailCreationWhenVersionIsWhitespace()
    {
        var result = CleanupItemVersion.Create("   ");

        result.IsFailure.Should().BeTrue();
        result.Error.IsValidationError().Should().BeTrue();
    }

    [Test]
    public void FailCreationWhenVersionContainsNonDigitSegments()
    {
        var result = CleanupItemVersion.Create("abc.1");

        result.IsFailure.Should().BeTrue();
        result.Error.IsValidationError().Should().BeTrue();
    }

    [Test]
    public void FailCreationWhenVersionHasTooManySegments()
    {
        var result = CleanupItemVersion.Create("1.2.3.4.5");

        result.IsFailure.Should().BeTrue();
        result.Error.IsValidationError().Should().BeTrue();
    }

    [Test]
    public void OrderOlderMajorVersionAsLowerWhenYearAndMinorDiffer()
    {
        var olderResult = CleanupItemVersion.Create("2023.3");
        var newerResult = CleanupItemVersion.Create("2024.1");
        var older = olderResult.Value;
        var newer = newerResult.Value;

        olderResult.IsSuccess.Should().BeTrue();
        newerResult.IsSuccess.Should().BeTrue();
        newer.Should().BeGreaterThan(older);
    }

    [Test]
    public void TreatVersionsAsEqualWhenStringsMatch()
    {
        var firstResult = CleanupItemVersion.Create("2024.1");
        var secondResult = CleanupItemVersion.Create("2024.1");
        var first = firstResult.Value;
        var second = secondResult.Value;

        firstResult.IsSuccess.Should().BeTrue();
        secondResult.IsSuccess.Should().BeTrue();
        first.CompareTo(second).Should().Be(0);
    }

    [Test]
    public void OrderPatchAboveMinorReleaseWhenPatchSegmentIsPresent()
    {
        var minorResult = CleanupItemVersion.Create("2024.1");
        var patchResult = CleanupItemVersion.Create("2024.1.1");
        var minor = minorResult.Value;
        var patch = patchResult.Value;

        minorResult.IsSuccess.Should().BeTrue();
        patchResult.IsSuccess.Should().BeTrue();
        minor.Should().BeLessThan(patch);
    }

    [Test]
    [Arguments("2024.1", "2024.1")]
    [Arguments("2024.1.0", "2024.1.0")]
    public void FormatVersionAsStringWhenVersionIsValid(string raw, string expected)
    {
        var result = CleanupItemVersion.Create(raw);
        var version = result.Value;

        result.IsSuccess.Should().BeTrue();
        version.ToString().Should().Be(expected);
    }
}

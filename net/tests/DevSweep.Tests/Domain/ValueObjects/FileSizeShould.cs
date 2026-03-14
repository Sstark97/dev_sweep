using AwesomeAssertions;
using DevSweep.Domain.ValueObjects;

namespace DevSweep.Tests.Domain.ValueObjects;

internal sealed class FileSizeShould
{
    [Test]
    public void FailWhenBytesAreNegative()
    {
        var result = FileSize.Create(-1);

        result.IsFailure.Should().BeTrue();
        result.Error.IsValidationError().Should().BeTrue();
        result.Error.MessageContains("negative").Should().BeTrue();
    }

    [Test]
    [Arguments(0)]
    [Arguments(1024)]
    public void SucceedWhenBytesAreNonNegative(long bytes)
    {
        var result = FileSize.Create(bytes);

        result.IsSuccess.Should().BeTrue();
    }

    [Test]
    public void ConvertBytesToKilobytes()
    {
        var result = FileSize.Create(2048);
        var fileSize = result.Value;

        result.IsSuccess.Should().BeTrue();
        fileSize.InKilobytes().Should().Be(2m);
    }

    [Test]
    public void ConvertBytesToMegabytes()
    {
        var result = FileSize.Create(1048576);
        var fileSize = result.Value;

        result.IsSuccess.Should().BeTrue();
        fileSize.InMegabytes().Should().Be(1m);
    }

    [Test]
    public void ConvertBytesToGigabytes()
    {
        var result = FileSize.Create(1073741824);
        var fileSize = result.Value;

        result.IsSuccess.Should().BeTrue();
        fileSize.InGigabytes().Should().Be(1m);
    }

    [Test]
    public void AddTwoSizes()
    {
        var smallSize = FileSize.Create(1024).Value;
        var largeSize = FileSize.Create(2048).Value;

        var sum = smallSize.Add(largeSize);

        sum.InKilobytes().Should().Be(3m);
    }

    [Test]
    [Arguments(512, "512 B")]
    [Arguments(2048, "2.00 KB")]
    [Arguments(2097152, "2.00 MB")]
    [Arguments(2147483648, "2.00 GB")]
    public void FormatWithAppropriateSuffix(long bytes, string expected)
    {
        var fileSize = FileSize.Create(bytes).Value;

        fileSize.ToString().Should().Be(expected);
    }

    [Test]
    public void IndicateSmallerSizeIsLess()
    {
        var small = FileSize.Create(1024).Value;
        var large = FileSize.Create(2048).Value;

        large.Should().BeGreaterThan(small);
        small.Should().BeLessThan(large);
    }

    [Test]
    public void CompareUsingCompareTo()
    {
        var small = FileSize.Create(1024).Value;
        var large = FileSize.Create(2048).Value;

        small.CompareTo(large).Should().BeNegative();
        large.CompareTo(small).Should().BePositive();
    }

    [Test]
    public void HaveZeroBytesForZero()
    {
        var zero = FileSize.Zero;

        zero.InKilobytes().Should().Be(0);
    }

    [Test]
    public void CreateFromMegabytes()
    {
        var result = FileSize.FromMegabytes(2);

        result.IsSuccess.Should().BeTrue();
        result.Value.InMegabytes().Should().Be(2);
    }

    [Test]
    public void FailFromMegabytesWhenNegative()
    {
        var result = FileSize.FromMegabytes(-1);

        result.IsFailure.Should().BeTrue();
        result.Error.MessageContains("negative").Should().BeTrue();
    }
}

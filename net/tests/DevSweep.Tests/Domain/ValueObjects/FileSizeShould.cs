using DevSweep.Domain.ValueObjects;

namespace DevSweep.Tests.Domain.ValueObjects;

public class FileSizeShould
{
    [Fact]
    public void FailWhenBytesAreNegative()
    {
        var result = FileSize.Create(-1);

        result.IsFailure.Should().BeTrue();
        result.Error.IsValidationError().Should().BeTrue();
        result.Error.MessageContains("negative").Should().BeTrue();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1024)]
    public void SucceedWhenBytesAreNonNegative(long bytes)
    {
        var result = FileSize.Create(bytes);

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public void ConvertBytesToKilobytes()
    {
        var result = FileSize.Create(2048);
        var fileSize = result.Value;

        result.IsSuccess.Should().BeTrue();
        fileSize.InKilobytes().Should().Be(2m);
    }

    [Fact]
    public void ConvertBytesToMegabytes()
    {
        var result = FileSize.Create(1048576);
        var fileSize = result.Value;

        result.IsSuccess.Should().BeTrue();
        fileSize.InMegabytes().Should().Be(1m);
    }

    [Fact]
    public void ConvertBytesToGigabytes()
    {
        var result = FileSize.Create(1073741824);
        var fileSize = result.Value;

        result.IsSuccess.Should().BeTrue();
        fileSize.InGigabytes().Should().Be(1m);
    }

    [Fact]
    public void AddTwoSizes()
    {
        var smallSize = FileSize.Create(1024).Value;
        var largeSize = FileSize.Create(2048).Value;

        var sum = smallSize.Add(largeSize);

        sum.InKilobytes().Should().Be(3m);
    }

    [Theory]
    [InlineData(512, "512 B")]
    [InlineData(2048, "2.00 KB")]
    [InlineData(2097152, "2.00 MB")]
    [InlineData(2147483648, "2.00 GB")]
    public void FormatWithAppropriateSuffix(long bytes, string expected)
    {
        var fileSize = FileSize.Create(bytes).Value;

        fileSize.ToString().Should().Be(expected);
    }

    [Fact]
    public void IndicateSmallerSizeIsLess()
    {
        var small = FileSize.Create(1024).Value;
        var large = FileSize.Create(2048).Value;

        large.Should().BeGreaterThan(small);
        small.Should().BeLessThan(large);
    }

    [Fact]
    public void CompareUsingCompareTo()
    {
        var small = FileSize.Create(1024).Value;
        var large = FileSize.Create(2048).Value;

        small.CompareTo(large).Should().BeNegative();
        large.CompareTo(small).Should().BePositive();
    }
}

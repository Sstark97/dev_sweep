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

    [Fact]
    public void SucceedWhenBytesAreZero()
    {
        var result = FileSize.Create(0);

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public void SucceedWhenBytesArePositive()
    {
        var result = FileSize.Create(1024);

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public void ConvertBytesToKilobytes()
    {
        var result = FileSize.Create(2048);

        result.IsSuccess.Should().BeTrue();
        result.Value.InKilobytes().Should().Be(2m);
    }

    [Fact]
    public void ConvertBytesToMegabytes()
    {
        var result = FileSize.Create(1048576);

        result.IsSuccess.Should().BeTrue();
        result.Value.InMegabytes().Should().Be(1m);
    }

    [Fact]
    public void ConvertBytesToGigabytes()
    {
        var result = FileSize.Create(1073741824);

        result.IsSuccess.Should().BeTrue();
        result.Value.InGigabytes().Should().Be(1m);
    }

    [Fact]
    public void AddTwoSizes()
    {
        var aSize = FileSize.Create(1024);
        var anotherSize = FileSize.Create(2048);

        aSize.IsSuccess.Should().BeTrue();
        anotherSize.IsSuccess.Should().BeTrue();

        var sum = aSize.Value.Add(anotherSize.Value);

        sum.InKilobytes().Should().Be(3m);
    }

    [Fact]
    public void FormatBytesWithoutDecimals()
    {
        var result = FileSize.Create(512);

        result.IsSuccess.Should().BeTrue();
        result.Value.ToString().Should().Be("512 B");
    }

    [Fact]
    public void FormatKilobytesWithTwoDecimals()
    {
        var result = FileSize.Create(2048);

        result.IsSuccess.Should().BeTrue();
        result.Value.ToString().Should().Be("2.00 KB");
    }

    [Fact]
    public void FormatMegabytesWithTwoDecimals()
    {
        var result = FileSize.Create(2097152);

        result.IsSuccess.Should().BeTrue();
        result.Value.ToString().Should().Be("2.00 MB");
    }

    [Fact]
    public void FormatGigabytesWithTwoDecimals()
    {
        var result = FileSize.Create(2147483648);

        result.IsSuccess.Should().BeTrue();
        result.Value.ToString().Should().Be("2.00 GB");
    }

    [Fact]
    public void IndicateSmallerSizeIsLess()
    {
        var smallerSize = FileSize.Create(1024);
        var largerSize = FileSize.Create(2048);

        smallerSize.IsSuccess.Should().BeTrue();
        largerSize.IsSuccess.Should().BeTrue();

        var small = smallerSize.Value;
        var large = largerSize.Value;

        large.Should().BeGreaterThan(small);
        small.Should().BeLessThan(large);
    }

    [Fact]
    public void CompareUsingCompareTo()
    {
        var smallerSize = FileSize.Create(1024);
        var largerSize = FileSize.Create(2048);

        smallerSize.IsSuccess.Should().BeTrue();
        largerSize.IsSuccess.Should().BeTrue();

        var small = smallerSize.Value;
        var large = largerSize.Value;

        small.CompareTo(large).Should().BeNegative();
        large.CompareTo(small).Should().BePositive();
    }
}

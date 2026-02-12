using DevSweep.Domain.ValueObjects;

namespace DevSweep.Tests.Domain.ValueObjects;

public class FilePathCreationShould
{
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void FailWhenPathIsMissingOrEmpty(string? invalidPath)
    {
        var result = FilePath.Create(invalidPath!);

        result.IsFailure.Should().BeTrue();
        result.Error.IsValidationError().Should().BeTrue();
        result.Error.MessageContains("empty").Should().BeTrue();
    }

    [Fact]
    public void FailWhenPathExceedsMaximumLength()
    {
        var longPath = new string('a', 261);
        var result = FilePath.Create(longPath);

        result.IsFailure.Should().BeTrue();
        result.Error.IsValidationError().Should().BeTrue();
        result.Error.MessageContains("exceeds maximum length").Should().BeTrue();
    }

    [Fact]
    public void SucceedWhenPathIsValid()
    {
        var result = FilePath.Create("/valid/path/file.txt");

        result.IsSuccess.Should().BeTrue();
        result.Value.ToString().Should().Be("/valid/path/file.txt");
    }

    [Fact]
    public void PreserveOriginalPathValue()
    {
        var originalPath = "/Users/test/documents/file.pdf";
        var result = FilePath.Create(originalPath);

        result.IsSuccess.Should().BeTrue();
        result.Value.ToString().Should().Be(originalPath);
    }

    [Fact]
    public void ExtractFileNameCorrectly()
    {
        var result = FilePath.Create("/path/to/file.txt");

        result.IsSuccess.Should().BeTrue();
        result.Value.FileName().Should().Be("file.txt");
    }

    [Fact]
    public void ExtractDirectoryPathCorrectly()
    {
        var result = FilePath.Create("/path/to/file.txt");

        result.IsSuccess.Should().BeTrue();
        result.Value.DirectoryPath().Should().Be("/path/to");
    }

    [Fact]
    public void ExtractExtensionCorrectly()
    {
        var result = FilePath.Create("/path/to/file.txt");

        result.IsSuccess.Should().BeTrue();
        result.Value.Extension().Should().Be(".txt");
    }

    [Fact]
    public void SupportValueEqualitySemantics()
    {
        var aPath = FilePath.Create("/path/to/file.txt");
        var anotherPath = FilePath.Create("/path/to/file.txt");

        aPath.IsSuccess.Should().BeTrue();
        anotherPath.IsSuccess.Should().BeTrue();

        var firstValue = aPath.Value;
        var secondValue = anotherPath.Value;

        firstValue.Should().Be(secondValue);
        (firstValue == secondValue).Should().BeTrue();
    }
}

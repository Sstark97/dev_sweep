using AwesomeAssertions;
using DevSweep.Domain.ValueObjects;

namespace DevSweep.Tests.Domain.ValueObjects;

internal sealed class FilePathCreationShould
{
    [Test]
    [Arguments(null)]
    [Arguments("")]
    [Arguments("   ")]
    public void FailWhenPathIsMissingOrEmpty(string? invalidPath)
    {
        var result = FilePath.Create(invalidPath!);

        result.IsFailure.Should().BeTrue();
        result.Error.IsValidationError().Should().BeTrue();
        result.Error.MessageContains("empty").Should().BeTrue();
    }

    [Test]
    public void FailWhenPathExceedsMaximumLength()
    {
        var longPath = new string('a', 261);
        var result = FilePath.Create(longPath);

        result.IsFailure.Should().BeTrue();
        result.Error.IsValidationError().Should().BeTrue();
        result.Error.MessageContains("exceeds maximum length").Should().BeTrue();
    }

    [Test]
    public void SucceedWhenPathIsValid()
    {
        var path = Path.Combine("valid", "path", "file.txt");
        var result = FilePath.Create(path);

        result.IsSuccess.Should().BeTrue();
    }

    [Test]
    public void PreserveOriginalPathValue()
    {
        var originalPath = Path.Combine("documents", "report.pdf");
        var result = FilePath.Create(originalPath);
        var filePath = result.Value;

        result.IsSuccess.Should().BeTrue();
        filePath.ToString().Should().Be(originalPath);
    }

    [Test]
    public void ExtractFileNameCorrectly()
    {
        var path = Path.Combine("path", "to", "file.txt");
        var result = FilePath.Create(path);
        var filePath = result.Value;

        result.IsSuccess.Should().BeTrue();
        filePath.FileName().Should().Be("file.txt");
    }

    [Test]
    public void ExtractDirectoryPathCorrectly()
    {
        var path = Path.Combine("path", "to", "file.txt");
        var result = FilePath.Create(path);
        var filePath = result.Value;

        result.IsSuccess.Should().BeTrue();
        var expectedDirectoryPath = Path.Combine("path", "to");
        filePath.DirectoryPath().Should().Be(expectedDirectoryPath);
    }

    [Test]
    public void ExtractExtensionCorrectly()
    {
        var path = Path.Combine("path", "to", "file.txt");
        var result = FilePath.Create(path);
        var filePath = result.Value;

        result.IsSuccess.Should().BeTrue();
        filePath.Extension().Should().Be(".txt");
    }

    [Test]
    public void SupportValueEqualitySemantics()
    {
        var firstValue = FilePath.Create(Path.Combine("path","to", "file.txt")).Value;
        var secondValue = FilePath.Create(Path.Combine("path", "to", "file.txt")).Value;

        firstValue.Should().Be(secondValue);
        (firstValue == secondValue).Should().BeTrue();
    }
}

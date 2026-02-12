using DevSweep.Domain.Common;
using DevSweep.Domain.ValueObjects;

namespace DevSweep.Tests.Domain.ValueObjects;

public class CleanupResultShould
{
    [Fact]
    public void FailWhenFilesDeletedIsNegative()
    {
        var zeroSize = FileSize.Create(0).Value;

        var result = CleanupResult.Create(-1, zeroSize);

        Assert.True(result.IsFailure);
        Assert.True(result.Error.MessageContains("negative"));
    }

    [Fact]
    public void SucceedWhenFilesDeletedIsZero()
    {
        var sizeResult = FileSize.Create(0);
        sizeResult.IsSuccess.Should().BeTrue();

        var result = CleanupResult.Create(0, sizeResult.Value);

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public void SucceedWithValidParameters()
    {
        var validFileCount = 5;
        var sizeResult = FileSize.Create(1024);
        sizeResult.IsSuccess.Should().BeTrue();

        var result = CleanupResult.Create(validFileCount, sizeResult.Value);

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public void IndicateWhenErrorsExist()
    {
        var sizeResult = FileSize.Create(0);
        sizeResult.IsSuccess.Should().BeTrue();
        var errorMessages = new List<string> { "Failed to delete file" };

        var result = CleanupResult.CreateWithErrors(0, sizeResult.Value, errorMessages);
        result.IsSuccess.Should().BeTrue();

        var cleanupResult = result.Value;
        cleanupResult.HasErrors().Should().BeTrue();
    }

    [Fact]
    public void IndicateWhenNoErrorsExist()
    {
        var sizeResult = FileSize.Create(0);
        sizeResult.IsSuccess.Should().BeTrue();

        var result = CleanupResult.Create(0, sizeResult.Value);
        result.IsSuccess.Should().BeTrue();

        var cleanupResult = result.Value;
        cleanupResult.HasErrors().Should().BeFalse();
    }

    [Fact]
    public void ProvideTotalFilesDeleted()
    {
        var expectedFileCount = 10;
        var smallSize = FileSize.Create(2048).Value;
        var largeSize = FileSize.Create(4096).Value;
        var firstResult = CleanupResult.Create(expectedFileCount, smallSize).Value;
        var secondResult = CleanupResult.Create(5, largeSize).Value;

        var combined = firstResult.Combine(secondResult);

        combined.TotalFilesDeleted().Should().Be(expectedFileCount + 5);
    }

    [Fact]
    public void ProvideTotalSpaceFreed()
    {
        var smallSize = FileSize.Create(1024).Value;
        var largeSize = FileSize.Create(4096).Value;
        var firstResult = CleanupResult.Create(3, smallSize).Value;
        var secondResult = CleanupResult.Create(5, largeSize).Value;

        var combined = firstResult.Combine(secondResult);

        combined.TotalSpaceFreed().Should().Be(smallSize.Add(largeSize));
    }

    [Fact]
    public void ProvideErrorMessages()
    {
        var zeroSize = FileSize.Create(0).Value;
        var firstError = "Permission denied";
        var secondError = "File locked";
        var expectedErrors = new List<string> { firstError, secondError };

        var result = CleanupResult.CreateWithErrors(0, zeroSize, expectedErrors);
        result.IsSuccess.Should().BeTrue();

        var cleanupResult = result.Value;
        var actualErrors = cleanupResult.ErrorMessages();
        actualErrors.Count.Should().Be(2);
        actualErrors.Should().Contain(firstError);
        actualErrors.Should().Contain(secondError);
    }

    [Fact]
    public void CombineTwoResults()
    {
        var smallSize = FileSize.Create(1024).Value;
        var largeSize = FileSize.Create(2048).Value;
        var firstResult = CleanupResult.Create(3, smallSize).Value;
        var secondResult = CleanupResult.Create(5, largeSize).Value;

        var combined = firstResult.Combine(secondResult);

        Assert.Equal(8, combined.TotalFilesDeleted());
        Assert.Equal(smallSize.Add(largeSize), combined.TotalSpaceFreed());
    }

    [Fact]
    public void AggregateMultipleResultsUsingLinq()
    {
        var smallSize = FileSize.Create(1024).Value;
        var mediumSize = FileSize.Create(2048).Value;
        var largeSize = FileSize.Create(4096).Value;

        var firstResult = CleanupResult.Create(2, smallSize).Value;
        var secondResult = CleanupResult.Create(3, mediumSize).Value;
        var thirdResult = CleanupResult.Create(4, largeSize).Value;

        var results = new[] { firstResult, secondResult, thirdResult };

        var aggregated = (
            from result in results
            select result
        ).Aggregate((accumulator, current) => accumulator.Combine(current));

        Assert.Equal(9, aggregated.TotalFilesDeleted());
        Assert.Equal(
            smallSize.Add(mediumSize).Add(largeSize),
            aggregated.TotalSpaceFreed()
        );
    }

    [Fact]
    public void CombineResultsWithErrors()
    {
        var zeroSize = FileSize.Create(0).Value;
        var firstError = "Error A";
        var secondError = "Error B";
        var resultWithFirstError = CleanupResult.CreateWithErrors(1, zeroSize, [firstError]).Value;
        var resultWithSecondError = CleanupResult.CreateWithErrors(2, zeroSize, [secondError]).Value;

        var combined = resultWithFirstError.Combine(resultWithSecondError);

        combined.HasErrors().Should().BeTrue();
        combined.ErrorMessages().Count.Should().Be(2);
        combined.ErrorMessages().Should().Contain(firstError);
        combined.ErrorMessages().Should().Contain(secondError);
    }
}

using DevSweep.Domain.ValueObjects;

namespace DevSweep.Tests.Domain.ValueObjects;

public class CleanupResultShould
{
    [Fact]
    public void FailWhenFilesDeletedIsNegative()
    {
        var zeroSize = FileSize.Create(0).Value;

        var result = CleanupResult.Create(-1, zeroSize);

        result.IsFailure.Should().BeTrue();
        result.Error.MessageContains("negative").Should().BeTrue();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(5)]
    public void SucceedWhenFilesDeletedIsNonNegative(int filesDeleted)
    {
        var validSize = FileSize.Create(1024).Value;

        var result = CleanupResult.Create(filesDeleted, validSize);

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public void IndicateWhenErrorsExist()
    {
        var zeroSize = FileSize.Create(0).Value;
        var errorMessages = new List<string> { "Failed to delete file" };

        var result = CleanupResult.CreateWithErrors(0, zeroSize, errorMessages);
        var cleanupResult = result.Value;

        result.IsSuccess.Should().BeTrue();
        cleanupResult.HasErrors().Should().BeTrue();
    }

    [Fact]
    public void IndicateWhenNoErrorsExist()
    {
        var zeroSize = FileSize.Create(0).Value;

        var result = CleanupResult.Create(0, zeroSize);
        var cleanupResult = result.Value;

        result.IsSuccess.Should().BeTrue();
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
        var cleanupResult = result.Value;
        var actualErrors = cleanupResult.ErrorMessages();

        result.IsSuccess.Should().BeTrue();
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

        combined.TotalFilesDeleted().Should().Be(8);
        combined.TotalSpaceFreed().Should().Be(smallSize.Add(largeSize));
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

        aggregated.TotalFilesDeleted().Should().Be(9);
        aggregated.TotalSpaceFreed().Should().Be(smallSize.Add(mediumSize).Add(largeSize));
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

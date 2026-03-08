using AwesomeAssertions;
using DevSweep.Domain.Common;

namespace DevSweep.Tests.Domain.Common;

internal sealed class CollectExtensionsShould
{
    [Test]
    public void CollectGathersItemsWhenAllMappingsSucceed()
    {
        var source = new[] { 1, 2, 3 };

        var result = source.Collect(x => Result<int, string>.Success(x * 2));

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeEquivalentTo([2, 4, 6]);
    }

    [Test]
    public void FailCollectionWhenFirstMappingFails()
    {
        var source = new[] { 1, 2, 3 };

        var result = source.Collect(x =>
            x == 2
                ? Result<int, string>.Failure("error on 2")
                : Result<int, string>.Success(x));

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be("error on 2");
    }

    [Test]
    public void CollectSucceedsWithEmptyResultWhenSourceIsEmpty()
    {
        var result = Array.Empty<int>().Collect(x => Result<int, string>.Success(x));

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeEmpty();
    }

    [Test]
    public void CollectManyMergesNestedItemsWhenAllMappingsSucceed()
    {
        var source = new[] { "a", "b" };

        var result = source.CollectMany(s =>
            Result<IReadOnlyList<string>, string>.Success([s + "1", s + "2"]));

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeEquivalentTo(["a1", "a2", "b1", "b2"]);
    }

    [Test]
    public void FailCollectManyWhenFirstMappingFails()
    {
        var source = new[] { "a", "b", "c" };

        var result = source.CollectMany(s =>
            s == "b"
                ? Result<IReadOnlyList<string>, string>.Failure("error on b")
                : Result<IReadOnlyList<string>, string>.Success([s]));

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be("error on b");
    }

    [Test]
    public void CollectManySucceedsWithEmptyResultWhenSourceIsEmpty()
    {
        var result = Array.Empty<string>().CollectMany(
            s => Result<IReadOnlyList<string>, string>.Success([s]));

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeEmpty();
    }

    [Test]
    public void CollectManyAggregatesItemsWhenMultipleMappingsProvideElements()
    {
        var source = new[] { 1, 2, 3 };

        var result = source.CollectMany(x =>
            Result<IReadOnlyList<int>, string>.Success([x, x * 10]));

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(6);
    }
}

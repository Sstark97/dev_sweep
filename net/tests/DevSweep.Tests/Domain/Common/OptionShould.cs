using AwesomeAssertions;
using DevSweep.Domain.Common;
using System.Globalization;

namespace DevSweep.Tests.Domain.Common;

internal sealed class OptionShould
{
    [Test]
    public void BeSomeWhenCreatedWithValue()
    {
        var option = Option<int>.Some(42);

        option.IsSome.Should().BeTrue();
        option.IsNone.Should().BeFalse();
    }

    [Test]
    public void BeNoneWhenCreatedAsNone()
    {
        var option = Option<int>.None;

        option.IsNone.Should().BeTrue();
        option.IsSome.Should().BeFalse();
    }

    [Test]
    public void MatchSomeBranchWhenIsSome()
    {
        var option = Option<int>.Some(10);

        var result = option.Match(onSome: v => v * 2, onNone: () => -1);

        result.Should().Be(20);
    }

    [Test]
    public void MatchNoneBranchWhenIsNone()
    {
        var option = Option<int>.None;

        var result = option.Match(onSome: v => v * 2, onNone: () => -1);

        result.Should().Be(-1);
    }

    [Test]
    public void MapValueWhenIsSome()
    {
        var option = Option<string>.Some("hello");

        var mapped = option.Map(s => s.Length);

        mapped.Match(onSome: v => v, onNone: () => 0).Should().Be(5);
    }

    [Test]
    public void PropagateNoneOnMap()
    {
        var option = Option<string>.None;

        var mapped = option.Map(s => s.Length);

        mapped.IsNone.Should().BeTrue();
    }

    [Test]
    public void BindToSomeWhenIsSomeAndBinderReturnsSome()
    {
        var option = Option<int>.Some(5);

        var bound = option.Bind(v => Option<string>.Some(v.ToString(CultureInfo.InvariantCulture)));

        bound.Match(onSome: s => s, onNone: () => "none").Should().Be("5");
    }

    [Test]
    public void PropagateNoneOnBind()
    {
        var option = Option<int>.None;

        var bound = option.Bind(v => Option<string>.Some(v.ToString(CultureInfo.InvariantCulture)));

        bound.IsNone.Should().BeTrue();
    }

    [Test]
    public void ReturnNoneWhenBinderReturnsNone()
    {
        var option = Option<int>.Some(5);

        var bound = option.Bind(_ => Option<string>.None);

        bound.IsNone.Should().BeTrue();
    }

    [Test]
    public void FilterKeepsSomeWhenPredicateMatches()
    {
        var option = Option<int>.Some(10);

        var filtered = option.Filter(v => v > 5);

        filtered.IsSome.Should().BeTrue();
    }

    [Test]
    public void FilterReturnNoneWhenPredicateDoesNotMatch()
    {
        var option = Option<int>.Some(3);

        var filtered = option.Filter(v => v > 5);

        filtered.IsNone.Should().BeTrue();
    }

    [Test]
    public void ComposeWithLinqQuerySyntax()
    {
        var optionA = Option<int>.Some(4);
        var optionB = Option<int>.Some(6);

        var result =
            from a in optionA
            from b in optionB
            select a + b;

        result.Match(onSome: v => v, onNone: () => 0).Should().Be(10);
    }

    [Test]
    public void PropagateNoneInLinqQueryWhenAnyIsNone()
    {
        var optionA = Option<int>.Some(4);
        var optionB = Option<int>.None;

        var result =
            from a in optionA
            from b in optionB
            select a + b;

        result.IsNone.Should().BeTrue();
    }

    [Test]
    public void FilterWithWhereInLinqQuerySyntax()
    {
        var option = Option<int>.Some(10);

        var result =
            from v in option
            where v > 5
            select v * 2;

        result.Match(onSome: v => v, onNone: () => 0).Should().Be(20);
    }

    [Test]
    public void ReturnNoneWhenWherePredicateFailsInLinqQuery()
    {
        var option = Option<int>.Some(3);

        var result =
            from v in option
            where v > 5
            select v * 2;

        result.IsNone.Should().BeTrue();
    }

    [Test]
    public void ConvertSuccessResultToSome()
    {
        var resultSuccess = Result<int, string>.Success(42);

        var option = resultSuccess.ToOption();

        option.IsSome.Should().BeTrue();
        option.Match(onSome: v => v, onNone: () => 0).Should().Be(42);
    }

    [Test]
    public void ConvertFailureResultToNone()
    {
        var resultFailure = Result<int, string>.Failure("error");

        var option = resultFailure.ToOption();

        option.IsNone.Should().BeTrue();
    }

    [Test]
    public void ReturnValueWhenIsSomeOnValueOr()
    {
        var option = Option<int>.Some(99);

        var result = option.ValueOr(0);

        result.Should().Be(99);
    }

    [Test]
    public void ReturnFallbackWhenIsNoneOnValueOr()
    {
        var option = Option<int>.None;

        var result = option.ValueOr(-1);

        result.Should().Be(-1);
    }
}

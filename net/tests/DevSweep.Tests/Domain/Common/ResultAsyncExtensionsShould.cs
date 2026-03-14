using AwesomeAssertions;
using DevSweep.Domain.Common;

namespace DevSweep.Tests.Domain.Common;

internal sealed class ResultAsyncExtensionsShould
{
    // --- BindAsync ---

    [Test]
    public async Task PropagateSuccessThroughBindAsync()
    {
        var result = Result<int, string>.Success(5);

        var bound = await result.BindAsync(value =>
            Task.FromResult(Result<string, string>.Success($"value:{value}")));

        bound.IsSuccess.Should().BeTrue();
        bound.Value.Should().Be("value:5");
    }

    [Test]
    public async Task PropagateFailureThroughBindAsync()
    {
        var result = Result<int, string>.Failure("original error");

        var bound = await result.BindAsync(value =>
            Task.FromResult(Result<string, string>.Success($"value:{value}")));

        bound.IsFailure.Should().BeTrue();
        bound.Error.Should().Be("original error");
    }

    [Test]
    public async Task PropagateSuccessThroughBindAsyncOnTask()
    {
        var resultTask = Task.FromResult(Result<int, string>.Success(10));

        var bound = await resultTask.BindAsync(value =>
            Task.FromResult(Result<string, string>.Success($"double:{value * 2}")));

        bound.IsSuccess.Should().BeTrue();
        bound.Value.Should().Be("double:20");
    }

    [Test]
    public async Task PropagateFailureThroughBindAsyncOnTask()
    {
        var resultTask = Task.FromResult(Result<int, string>.Failure("task error"));

        var bound = await resultTask.BindAsync(value =>
            Task.FromResult(Result<string, string>.Success($"double:{value * 2}")));

        bound.IsFailure.Should().BeTrue();
        bound.Error.Should().Be("task error");
    }

    // --- MapAsync ---

    [Test]
    public async Task TransformValueThroughMapAsync()
    {
        var result = Result<int, string>.Success(4);

        var mapped = await result.MapAsync(value => Task.FromResult(value * 3));

        mapped.IsSuccess.Should().BeTrue();
        mapped.Value.Should().Be(12);
    }

    [Test]
    public async Task PropagateFailureThroughMapAsync()
    {
        var result = Result<int, string>.Failure("map error");

        var mapped = await result.MapAsync(value => Task.FromResult(value * 3));

        mapped.IsFailure.Should().BeTrue();
        mapped.Error.Should().Be("map error");
    }

    [Test]
    public async Task TransformValueThroughMapAsyncOnTask()
    {
        var resultTask = Task.FromResult(Result<int, string>.Success(7));

        var mapped = await resultTask.MapAsync(value => value + 1);

        mapped.IsSuccess.Should().BeTrue();
        mapped.Value.Should().Be(8);
    }

    [Test]
    public async Task PropagateFailureThroughMapAsyncOnTask()
    {
        var resultTask = Task.FromResult(Result<int, string>.Failure("task map error"));

        var mapped = await resultTask.MapAsync(value => value + 1);

        mapped.IsFailure.Should().BeTrue();
        mapped.Error.Should().Be("task map error");
    }

    // --- TapAsync ---

    [Test]
    public async Task ExecuteSideEffectThroughTapAsync()
    {
        var sideEffectRan = false;
        var result = Result<int, string>.Success(42);

        var tapped = await result.TapAsync(value =>
        {
            sideEffectRan = true;
            return Task.FromResult(Result<Unit, string>.Success(Unit.Value));
        });

        tapped.IsSuccess.Should().BeTrue();
        tapped.Value.Should().Be(42);
        sideEffectRan.Should().BeTrue();
    }

    [Test]
    public async Task PropagateFailureFromTapAsync()
    {
        var result = Result<int, string>.Success(42);

        var tapped = await result.TapAsync(_ =>
            Task.FromResult(Result<Unit, string>.Failure("side effect failed")));

        tapped.IsFailure.Should().BeTrue();
        tapped.Error.Should().Be("side effect failed");
    }

    [Test]
    public async Task SkipSideEffectWhenFailureThroughTapAsync()
    {
        var sideEffectRan = false;
        var result = Result<int, string>.Failure("already failed");

        var tapped = await result.TapAsync(value =>
        {
            sideEffectRan = true;
            return Task.FromResult(Result<Unit, string>.Success(Unit.Value));
        });

        tapped.IsFailure.Should().BeTrue();
        tapped.Error.Should().Be("already failed");
        sideEffectRan.Should().BeFalse();
    }

    [Test]
    public async Task ExecuteSideEffectThroughTapAsyncOnTask()
    {
        var sideEffectRan = false;
        var resultTask = Task.FromResult(Result<int, string>.Success(5));

        var tapped = await resultTask.TapAsync(value =>
        {
            sideEffectRan = true;
            return Task.FromResult(Result<Unit, string>.Success(Unit.Value));
        });

        tapped.IsSuccess.Should().BeTrue();
        sideEffectRan.Should().BeTrue();
    }
}

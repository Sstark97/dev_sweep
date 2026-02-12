using DevSweep.Domain.Errors;

namespace DevSweep.Tests.Domain.Errors;

public class DomainErrorShould
{
    [Fact]
    public void CreateValidationError()
    {
        var error = DomainError.Validation("Invalid input");

        error.IsValidationError().Should().BeTrue();
        error.MessageContains("Invalid input").Should().BeTrue();
    }

    [Fact]
    public void CreateNotFoundError()
    {
        var error = DomainError.NotFound("User", "123");

        error.IsNotFoundError().Should().BeTrue();
        error.MessageContains("User").Should().BeTrue();
        error.MessageContains("123").Should().BeTrue();
    }

    [Fact]
    public void CreateInvalidOperationError()
    {
        var error = DomainError.InvalidOperation("Cannot perform this action");

        error.IsInvalidOperationError().Should().BeTrue();
        error.MessageContains("Cannot perform").Should().BeTrue();
    }

    [Fact]
    public void FormatErrorWithCodeAndMessage()
    {
        var error = DomainError.Validation("Test message");

        var formatted = error.ToString();

        formatted.Should().Contain("VALIDATION_ERROR");
        formatted.Should().Contain("Test message");
    }
}

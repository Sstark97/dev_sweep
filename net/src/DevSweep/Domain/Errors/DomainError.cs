namespace DevSweep.Domain.Errors;

public readonly record struct DomainError
{
    private readonly string code;
    private readonly string message;

    private DomainError(string code, string message)
    {
        this.code = code;
        this.message = message;
    }

    public static DomainError Validation(string message) =>
        new("VALIDATION_ERROR", message);

    public static DomainError NotFound(string entity, string id) =>
        new("NOT_FOUND", $"{entity} with ID {id} not found");

    public static DomainError InvalidOperation(string message) =>
        new("INVALID_OPERATION", message);

    public bool IsValidationError() => code == "VALIDATION_ERROR";
    public bool IsNotFoundError() => code == "NOT_FOUND";
    public bool IsInvalidOperationError() => code == "INVALID_OPERATION";

    public bool MessageContains(string text) => message.Contains(text, StringComparison.OrdinalIgnoreCase);

    public override string ToString() => $"[{code}] {message}";
}

using System.Diagnostics.CodeAnalysis;

[assembly: SuppressMessage("Design", "CA1000:Do not declare static members on generic types", Justification = "Result type requires static factory methods for ergonomics")]
[assembly: SuppressMessage("Performance", "CA1515:Consider making public types internal", Justification = "Domain types are intentionally public for use in other layers")]
[assembly: SuppressMessage("Design", "CA1716:Identifiers should not match keywords", Justification = "Error method name is consistent with the formatter's naming convention (Info, Success, Warning, Error, Debug)")]
[assembly: SuppressMessage("Performance", "CA1805:Do not initialize unnecessarily", Justification = "Unit.Value provides a named canonical instance for ergonomics even though it equals the type default")]

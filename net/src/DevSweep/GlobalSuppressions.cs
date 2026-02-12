using System.Diagnostics.CodeAnalysis;

[assembly: SuppressMessage("Design", "CA1000:Do not declare static members on generic types", Justification = "Result type requires static factory methods for ergonomics")]
[assembly: SuppressMessage("Performance", "CA1515:Consider making public types internal", Justification = "Domain types are intentionally public for use in other layers")]

using System.Globalization;
using DevSweep.Application.Models;
using DevSweep.Application.Ports.Driven;
using DevSweep.Domain.Entities;
using DevSweep.Domain.ValueObjects;
using Spectre.Console;

namespace DevSweep.Infrastructure.Cli.Output;

public sealed class SpectreOutputFormatter(IAnsiConsole console) : IOutputFormatter
{
    public void Info(string message) =>
        console.MarkupLine($"[blue][[INFO]][/] {Markup.Escape(message)}");

    public void Success(string message) =>
        console.MarkupLine($"[green][[OK]][/] {Markup.Escape(message)}");

    public void Warning(string message) =>
        console.MarkupLine($"[yellow][[WARN]][/] {Markup.Escape(message)}");

    public void Error(string message) =>
        console.MarkupLine($"[red][[ERROR]][/] {Markup.Escape(message)}");

    public void Debug(string message) =>
        console.MarkupLine($"[grey][[DEBUG]][/] {Markup.Escape(message)}");

    public void Section(string title) =>
        console.Write(new Rule($"[bold]{Markup.Escape(title)}[/]") { Style = Style.Parse("blue") });

    public void DisplayBanner(string version)
    {
        console.Write(new FigletText("DevSweep").Color(Color.Blue));
        console.MarkupLine($"[grey]v{Markup.Escape(version)}[/]");
    }

    public void DisplayAnalysisReport(AnalysisReport report)
    {
        var table = new Table()
            .Border(TableBorder.Rounded)
            .AddColumn("[bold]Module[/]")
            .AddColumn("[bold]Items[/]", c => c.RightAligned())
            .AddColumn("[bold]Safe[/]", c => c.RightAligned())
            .AddColumn("[bold]Unsafe[/]", c => c.RightAligned())
            .AddColumn("[bold]Size[/]", c => c.RightAligned());

        foreach (var module in report.Modules())
        {
            table.AddRow(
                Markup.Escape(module.Module.ToString()),
                module.ItemCount().ToString(CultureInfo.InvariantCulture),
                $"[green]{module.SafeItemCount().ToString(CultureInfo.InvariantCulture)}[/]",
                module.UnsafeItemCount() > 0 ? $"[yellow]{module.UnsafeItemCount().ToString(CultureInfo.InvariantCulture)}[/]" : "0",
                Markup.Escape(module.TotalSize().ToString()));
        }

        table.AddRow(
            "[bold]Total[/]",
            $"[bold]{report.TotalItemCount().ToString(CultureInfo.InvariantCulture)}[/]",
            string.Empty,
            string.Empty,
            $"[bold]{Markup.Escape(report.TotalSize().ToString())}[/]");

        console.Write(table);
    }

    public void DisplayCompletion(IReadOnlyList<CleanupSummary> summaries)
    {
        var table = new Table()
            .Border(TableBorder.Rounded)
            .AddColumn("[bold]Module[/]")
            .AddColumn("[bold]Files Deleted[/]", c => c.RightAligned())
            .AddColumn("[bold]Space Freed[/]", c => c.RightAligned())
            .AddColumn("[bold]Errors[/]", c => c.RightAligned());

        foreach (var summary in summaries)
        {
            var errorsDisplay = summary.Result.HasErrors()
                ? $"[red]{summary.Result.ErrorMessages().Count.ToString(CultureInfo.InvariantCulture)}[/]"
                : "[green]0[/]";

            table.AddRow(
                Markup.Escape(summary.Module.ToString()),
                summary.Result.TotalFilesDeleted().ToString(CultureInfo.InvariantCulture),
                Markup.Escape(summary.Result.TotalSpaceFreed().ToString()),
                errorsDisplay);
        }

        var totalFiles = summaries.Sum(s => s.Result.TotalFilesDeleted());
        var totalSize = summaries.Aggregate(
            FileSize.Zero,
            (acc, s) => acc.Add(s.Result.TotalSpaceFreed()));

        table.AddRow(
            "[bold]Total[/]",
            $"[bold]{totalFiles.ToString(CultureInfo.InvariantCulture)}[/]",
            $"[bold]{Markup.Escape(totalSize.ToString())}[/]",
            string.Empty);

        console.Write(table);
    }
}

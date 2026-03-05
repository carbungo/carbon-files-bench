using Spectre.Console;

namespace CarbonFiles.Benchmark.Rendering;

public static class SpectreRenderer
{
    private static readonly Dictionary<string, Color> CategoryColors = new()
    {
        ["Health"] = Color.Green,
        ["Buckets"] = Color.Blue,
        ["Files"] = Color.Cyan1,
        ["API Keys"] = Color.Yellow,
        ["Upload Tokens"] = Color.Orange1,
        ["Short URLs"] = Color.Magenta1,
        ["Dashboard"] = Color.Purple,
        ["Stats"] = Color.Teal,
        ["Large Transfers"] = Color.Orange3,
        ["Concurrency"] = Color.Red,
        ["SignalR Events"] = Color.DeepSkyBlue1,
    };

    public static void RenderHeader(string url)
    {
        AnsiConsole.Write(new FigletText("CF Benchmark").Color(Color.Cyan1));
        AnsiConsole.MarkupLine($"[grey]Target:[/] [bold]{url.EscapeMarkup()}[/]");
        AnsiConsole.MarkupLine($"[grey]Started:[/] [bold]{DateTime.UtcNow:u}[/]");
        AnsiConsole.WriteLine();
    }

    public static void RenderCategoryHeader(string category)
    {
        var color = CategoryColors.GetValueOrDefault(category, Color.White);
        AnsiConsole.Write(new Rule($"[bold {color}]{category.EscapeMarkup()}[/]").LeftJustified());
    }

    public static void RenderResults(List<BenchmarkResult> results)
    {
        var categories = results.Select(r => r.Category).Distinct().ToList();

        foreach (var category in categories)
        {
            var color = CategoryColors.GetValueOrDefault(category, Color.White);
            AnsiConsole.WriteLine();
            AnsiConsole.Write(new Rule($"[bold {color}]{category.EscapeMarkup()}[/]").LeftJustified());

            var table = new Table()
                .Border(TableBorder.Rounded)
                .BorderColor(color)
                .AddColumn(new TableColumn("[bold]Operation[/]").LeftAligned())
                .AddColumn(new TableColumn("[bold]Min[/]").RightAligned())
                .AddColumn(new TableColumn("[bold]Median[/]").RightAligned())
                .AddColumn(new TableColumn("[bold]P95[/]").RightAligned())
                .AddColumn(new TableColumn("[bold]P99[/]").RightAligned())
                .AddColumn(new TableColumn("[bold]Max[/]").RightAligned())
                .AddColumn(new TableColumn("[bold]Ops/s[/]").RightAligned())
                .AddColumn(new TableColumn("[bold]Throughput[/]").RightAligned())
                .AddColumn(new TableColumn("[bold]Status[/]").Centered());

            var categoryResults = results.Where(r => r.Category == category).ToList();
            foreach (var r in categoryResults)
            {
                var status = r.Success ? "[green]PASS[/]" : $"[red]FAIL[/]";

                if (!r.Success)
                {
                    table.AddRow(
                        r.Operation.EscapeMarkup(),
                        "-", "-", "-", "-", "-", "-", "-",
                        status
                    );
                    continue;
                }

                var sorted = r.LatenciesMs;
                var hasTimings = sorted.Count > 0;

                table.AddRow(
                    r.Operation.EscapeMarkup(),
                    hasTimings ? FormatMs(Statistics.Min(sorted)) : "-",
                    hasTimings ? FormatMs(Statistics.Median(sorted)) : "-",
                    hasTimings ? FormatMs(Statistics.P95(sorted)) : "-",
                    hasTimings ? FormatMs(Statistics.P99(sorted)) : "-",
                    hasTimings ? FormatMs(Statistics.Max(sorted)) : "-",
                    hasTimings ? FormatOps(Statistics.OpsPerSec(sorted)) : "-",
                    r.ThroughputMbPerSec.HasValue ? $"{r.ThroughputMbPerSec.Value:F2} MB/s" : "-",
                    status
                );
            }

            AnsiConsole.Write(table);
        }
    }

    public static void RenderScorecard(List<BenchmarkResult> results)
    {
        AnsiConsole.WriteLine();
        AnsiConsole.Write(new Rule("[bold yellow]Scorecard[/]").LeftJustified());

        var total = results.Count;
        var passed = results.Count(r => r.Success);
        var failed = total - passed;

        var grid = new Grid()
            .AddColumn()
            .AddColumn();

        grid.AddRow("[bold]Total Benchmarks:[/]", $"[bold]{total}[/]");
        grid.AddRow("[bold]Passed:[/]", $"[green bold]{passed}[/]");
        grid.AddRow("[bold]Failed:[/]", failed > 0 ? $"[red bold]{failed}[/]" : $"[green bold]{failed}[/]");

        var allLatencies = results
            .Where(r => r.Success && r.LatenciesMs.Count > 0)
            .SelectMany(r => r.LatenciesMs)
            .Where(l => l >= 0)
            .ToList();

        if (allLatencies.Count > 0)
        {
            allLatencies.Sort();
            grid.AddRow("[bold]Global Median:[/]", $"[bold]{FormatMs(Statistics.Median(allLatencies))}[/]");
            grid.AddRow("[bold]Global P95:[/]", $"[bold]{FormatMs(Statistics.P95(allLatencies))}[/]");
            grid.AddRow("[bold]Global P99:[/]", $"[bold]{FormatMs(Statistics.P99(allLatencies))}[/]");
        }

        grid.AddRow("[bold]Completed:[/]", $"[bold]{DateTime.UtcNow:u}[/]");
        AnsiConsole.Write(grid);

        // Failures detail
        var failures = results.Where(r => !r.Success).ToList();
        if (failures.Count > 0)
        {
            AnsiConsole.WriteLine();
            AnsiConsole.Write(new Rule("[bold red]Failures[/]").LeftJustified());
            foreach (var f in failures)
            {
                AnsiConsole.MarkupLine($"  [red]x[/] [bold]{f.Category.EscapeMarkup()}[/] / {f.Operation.EscapeMarkup()}");
                if (f.Error != null)
                    AnsiConsole.MarkupLine($"    [grey]{f.Error.EscapeMarkup()}[/]");
            }
        }
    }

    private static string FormatMs(double ms) => ms switch
    {
        < 1 => $"{ms:F3} ms",
        < 1000 => $"{ms:F1} ms",
        _ => $"{ms / 1000.0:F2} s"
    };

    private static string FormatOps(double ops) => ops switch
    {
        >= 1000 => $"{ops:F0}",
        >= 1 => $"{ops:F1}",
        _ => $"{ops:F3}"
    };
}

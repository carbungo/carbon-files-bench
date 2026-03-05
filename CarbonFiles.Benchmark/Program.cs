using CarbonFiles.Benchmark;
using CarbonFiles.Benchmark.Benchmarks;
using CarbonFiles.Benchmark.Rendering;
using CarbonFiles.Client;
using Spectre.Console;
using Spectre.Console.Cli;
using System.ComponentModel;

var app = new CommandApp<BenchmarkCommand>();
return app.Run(args);

internal sealed class BenchmarkSettings : CommandSettings
{
    [CommandOption("--url <URL>")]
    [Description("CarbonFiles server URL")]
    public required string Url { get; init; }

    [CommandOption("--key <KEY>")]
    [Description("Admin API key")]
    public required string Key { get; init; }

    [CommandOption("--iterations <N>")]
    [Description("Iterations per benchmark (default: 5)")]
    [DefaultValue(5)]
    public int Iterations { get; init; } = 5;

    [CommandOption("--skip-signalr")]
    [Description("Skip SignalR event benchmarks")]
    [DefaultValue(false)]
    public bool SkipSignalR { get; init; }

    [CommandOption("--max-upload-mb <MB>")]
    [Description("Max file size in MB for large transfer benchmarks (default: 100)")]
    [DefaultValue(100)]
    public int MaxUploadMb { get; init; } = 100;

    [CommandOption("--category <CATEGORY>")]
    [Description("Run only a specific category (e.g. Health, Buckets, Files, Large Transfers)")]
    public string? Category { get; init; }
}

internal sealed class BenchmarkCommand : AsyncCommand<BenchmarkSettings>
{
    private static readonly (string Name, Func<BenchmarkContext, Task> Run)[] AllBenchmarks =
    [
        ("Health", HealthBenchmarks.RunAsync),
        ("Buckets", BucketBenchmarks.RunAsync),
        ("Files", FileBenchmarks.RunAsync),
        ("API Keys", ApiKeyBenchmarks.RunAsync),
        ("Upload Tokens", UploadTokenBenchmarks.RunAsync),
        ("Short URLs", ShortUrlBenchmarks.RunAsync),
        ("Dashboard", DashboardBenchmarks.RunAsync),
        ("Stats", StatsBenchmarks.RunAsync),
        ("Large Transfers", LargeTransferBenchmarks.RunAsync),
        ("Concurrency", ConcurrencyBenchmarks.RunAsync),
        ("SignalR Events", SignalRBenchmarks.RunAsync),
    ];

    public override async Task<int> ExecuteAsync(CommandContext commandContext, BenchmarkSettings settings)
    {
        var url = settings.Url.TrimEnd('/');

        SpectreRenderer.RenderHeader(url);

        var client = new CarbonFilesClient(url, settings.Key);

        var ctx = new BenchmarkContext
        {
            Client = client,
            BaseUrl = url,
            AdminKey = settings.Key,
            Iterations = settings.Iterations,
            MaxUploadMb = settings.MaxUploadMb,
        };

        // Connectivity check
        try
        {
            await AnsiConsole.Status()
                .Spinner(Spinner.Known.Dots)
                .StartAsync("Checking connectivity...", async _ =>
                {
                    await client.Health.CheckAsync();
                });
            AnsiConsole.MarkupLine("[green]Connected successfully.[/]");
            AnsiConsole.WriteLine();
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine($"[red bold]Failed to connect:[/] {ex.Message.EscapeMarkup()}");
            return 1;
        }

        var benchmarks = AllBenchmarks.AsEnumerable();

        if (settings.SkipSignalR)
            benchmarks = benchmarks.Where(b => b.Name != "SignalR Events");

        if (!string.IsNullOrEmpty(settings.Category))
            benchmarks = benchmarks.Where(b =>
                b.Name.Equals(settings.Category, StringComparison.OrdinalIgnoreCase));

        var benchmarkList = benchmarks.ToList();

        if (benchmarkList.Count == 0)
        {
            AnsiConsole.MarkupLine("[yellow]No benchmarks matched the specified category.[/]");
            return 1;
        }

        foreach (var (name, run) in benchmarkList)
        {
            SpectreRenderer.RenderCategoryHeader(name);

            await AnsiConsole.Status()
                .Spinner(Spinner.Known.Dots)
                .SpinnerStyle(Style.Parse("cyan"))
                .StartAsync($"Running {name} benchmarks...", async _ =>
                {
                    try
                    {
                        await run(ctx);
                    }
                    catch (Exception ex)
                    {
                        ctx.Results.Add(new BenchmarkResult
                        {
                            Category = name,
                            Operation = $"[Category Error]",
                            Success = false,
                            Error = ex.Message,
                        });
                    }
                });

            // Render results for this category immediately
            var categoryResults = ctx.Results.Where(r => r.Category == name).ToList();
            if (categoryResults.Count > 0)
            {
                var passed = categoryResults.Count(r => r.Success);
                var total = categoryResults.Count;
                AnsiConsole.MarkupLine(
                    $"  [grey]{passed}/{total} passed[/]");
            }
        }

        // Final report
        SpectreRenderer.RenderResults(ctx.Results);
        SpectreRenderer.RenderScorecard(ctx.Results);

        return ctx.Results.Any(r => !r.Success) ? 1 : 0;
    }
}

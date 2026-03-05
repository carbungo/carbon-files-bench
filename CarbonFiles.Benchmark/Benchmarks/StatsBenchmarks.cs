namespace CarbonFiles.Benchmark.Benchmarks;

public static class StatsBenchmarks
{
    private const string Category = "Stats";

    public static async Task RunAsync(BenchmarkContext ctx)
    {
        await ctx.MeasureAsync(Category, "Get Stats", async () =>
        {
            await ctx.Client.Stats.GetAsync();
        });
    }
}

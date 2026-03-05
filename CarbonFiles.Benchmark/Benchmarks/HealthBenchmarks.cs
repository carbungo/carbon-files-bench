namespace CarbonFiles.Benchmark.Benchmarks;

public static class HealthBenchmarks
{
    private const string Category = "Health";

    public static async Task RunAsync(BenchmarkContext ctx)
    {
        await ctx.MeasureAsync(Category, "Health Check", async () =>
        {
            await ctx.Client.Health.CheckAsync();
        });
    }
}

using CarbonFiles.Client;
using CarbonFiles.Client.Models;

namespace CarbonFiles.Benchmark.Benchmarks;

public static class DashboardBenchmarks
{
    private const string Category = "Dashboard";

    public static async Task RunAsync(BenchmarkContext ctx)
    {
        string? token = null;
        await ctx.MeasureAsync(Category, "Create Dashboard Token", async () =>
        {
            var resp = await ctx.Client.Dashboard.CreateTokenAsync(
                new CreateDashboardTokenRequest());
            token = resp.Token;
        });

        if (token != null)
        {
            var dashClient = new CarbonFilesClient(ctx.BaseUrl, token);

            await ctx.MeasureAsync(Category, "Get Current User", async () =>
            {
                await dashClient.Dashboard.GetCurrentUserAsync();
            });
        }
    }
}

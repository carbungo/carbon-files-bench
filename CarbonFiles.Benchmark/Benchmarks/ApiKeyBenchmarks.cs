using CarbonFiles.Client.Models;

namespace CarbonFiles.Benchmark.Benchmarks;

public static class ApiKeyBenchmarks
{
    private const string Category = "API Keys";

    public static async Task RunAsync(BenchmarkContext ctx)
    {
        var createdPrefixes = new List<string>();

        // Create
        await ctx.MeasureAsync(Category, "Create API Key", async () =>
        {
            var resp = await ctx.Client.Keys.CreateAsync(new CreateApiKeyRequest
            {
                Name = $"bench-key-{Guid.NewGuid():N}"
            });
            createdPrefixes.Add(resp.Prefix);
        });

        // List
        await ctx.MeasureAsync(Category, "List API Keys", async () =>
        {
            await ctx.Client.Keys.ListAsync();
        });

        // Get usage
        if (createdPrefixes.Count > 0)
        {
            var prefix = createdPrefixes[0];
            await ctx.MeasureAsync(Category, "Get Key Usage", async () =>
            {
                await ctx.Client.Keys[prefix].GetUsageAsync();
            });
        }

        // Revoke
        foreach (var prefix in createdPrefixes)
        {
            await ctx.MeasureOnceAsync(Category, "Revoke API Key", async () =>
            {
                await ctx.Client.Keys[prefix].RevokeAsync();
            });
        }
    }
}

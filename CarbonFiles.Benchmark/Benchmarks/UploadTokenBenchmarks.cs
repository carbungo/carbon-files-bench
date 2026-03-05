using CarbonFiles.Client.Models;

namespace CarbonFiles.Benchmark.Benchmarks;

public static class UploadTokenBenchmarks
{
    private const string Category = "Upload Tokens";

    public static async Task RunAsync(BenchmarkContext ctx)
    {
        var bucket = await ctx.Client.Buckets.CreateAsync(new CreateBucketRequest
        {
            Name = $"bench-token-{Guid.NewGuid():N}"
        });

        try
        {
            // Create upload token
            string? token = null;
            await ctx.MeasureAsync(Category, "Create Upload Token", async () =>
            {
                var resp = await ctx.Client.Buckets[bucket.Id].Tokens.CreateAsync(
                    new CreateUploadTokenRequest());
                token = resp.Token;
            });

            // Upload using the token via the uploadToken parameter
            if (token != null)
            {
                var data = new byte[4096];
                Random.Shared.NextBytes(data);
                var capturedToken = token;

                await ctx.MeasureAsync(Category, "Upload with Token", async () =>
                {
                    await ctx.Client.Buckets[bucket.Id].Files
                        .UploadAsync(data, $"token-{Guid.NewGuid():N}.bin", uploadToken: capturedToken);
                });
            }
        }
        finally
        {
            await ctx.Client.Buckets[bucket.Id].DeleteAsync();
        }
    }
}

using CarbonFiles.Client.Models;

namespace CarbonFiles.Benchmark.Benchmarks;

public static class ShortUrlBenchmarks
{
    private const string Category = "Short URLs";

    public static async Task RunAsync(BenchmarkContext ctx)
    {
        var bucket = await ctx.Client.Buckets.CreateAsync(new CreateBucketRequest
        {
            Name = $"bench-short-{Guid.NewGuid():N}"
        });

        try
        {
            var data = new byte[1024];
            Random.Shared.NextBytes(data);
            var uploaded = await ctx.Client.Buckets[bucket.Id].Files
                .UploadAsync(data, "short-test.bin");

            var shortCode = uploaded.Uploaded.FirstOrDefault()?.ShortCode;

            if (!string.IsNullOrEmpty(shortCode))
            {
                // Resolve short URL via HTTP GET
                using var http = new HttpClient(new HttpClientHandler
                {
                    AllowAutoRedirect = false
                })
                { BaseAddress = new Uri(ctx.BaseUrl) };

                await ctx.MeasureAsync(Category, "Resolve Short URL", async () =>
                {
                    await http.GetAsync($"/s/{shortCode}");
                });

                // Delete short URL
                await ctx.MeasureOnceAsync(Category, "Delete Short URL", async () =>
                {
                    await ctx.Client.ShortUrls[shortCode].DeleteAsync();
                });
            }
        }
        finally
        {
            await ctx.Client.Buckets[bucket.Id].DeleteAsync();
        }
    }
}

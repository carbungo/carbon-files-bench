using CarbonFiles.Client.Models;

namespace CarbonFiles.Benchmark.Benchmarks;

public static class LargeTransferBenchmarks
{
    private const string Category = "Large Transfers";

    private static readonly int[] TiersMb = [1, 10, 50, 100, 250, 500, 1024];

    public static async Task RunAsync(BenchmarkContext ctx)
    {
        var bucket = await ctx.Client.Buckets.CreateAsync(new CreateBucketRequest
        {
            Name = $"bench-large-{Guid.NewGuid():N}"
        });
        var files = ctx.Client.Buckets[bucket.Id].Files;

        try
        {
            var tiers = TiersMb.Where(mb => mb <= ctx.MaxUploadMb).ToList();
            if (tiers.Count == 0)
                tiers = [ctx.MaxUploadMb];

            // Always include the configured max if it's not already a tier
            if (!tiers.Contains(ctx.MaxUploadMb))
                tiers.Add(ctx.MaxUploadMb);

            foreach (var sizeMb in tiers)
            {
                var sizeBytes = sizeMb * 1024L * 1024;
                var label = sizeMb >= 1024 ? $"{sizeMb / 1024.0:G3} GB" : $"{sizeMb} MB";

                // Fewer iterations for larger files
                var iterations = sizeMb switch
                {
                    <= 10 => 3,
                    <= 100 => 2,
                    _ => 1
                };

                // Generate random data
                var data = new byte[sizeBytes];
                Random.Shared.NextBytes(data);

                // Upload
                var uploadName = $"large-{sizeMb}mb-{Guid.NewGuid():N}.bin";
                await ctx.MeasureThroughputAsync(Category, $"Upload {label}", async () =>
                {
                    var ms = new MemoryStream(data);
                    await files.UploadAsync(ms, uploadName);
                    return sizeBytes;
                }, iterations: iterations);

                // Download the same file
                var fileRes = files[uploadName];
                await ctx.MeasureThroughputAsync(Category, $"Download {label}", async () =>
                {
                    var stream = await fileRes.DownloadAsync();
                    long total = 0;
                    var buffer = new byte[256 * 1024]; // 256 KB read buffer
                    int read;
                    while ((read = await stream.ReadAsync(buffer)) > 0)
                        total += read;
                    await stream.DisposeAsync();
                    return total;
                }, iterations: iterations);

                // Stream upload (tests chunked transfer path)
                await ctx.MeasureThroughputAsync(Category, $"Stream Upload {label}", async () =>
                {
                    var ms = new MemoryStream(data);
                    await files.UploadAsync(ms, $"stream-{sizeMb}mb-{Guid.NewGuid():N}.bin");
                    return sizeBytes;
                }, iterations: iterations);
            }
        }
        finally
        {
            await ctx.Client.Buckets[bucket.Id].DeleteAsync();
        }
    }
}

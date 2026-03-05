using CarbonFiles.Client.Models;

namespace CarbonFiles.Benchmark.Benchmarks;

public static class BucketBenchmarks
{
    private const string Category = "Buckets";

    public static async Task RunAsync(BenchmarkContext ctx)
    {
        var createdIds = new List<string>();

        // Create
        await ctx.MeasureAsync(Category, "Create Bucket", async () =>
        {
            var bucket = await ctx.Client.Buckets.CreateAsync(new CreateBucketRequest
            {
                Name = $"bench-{Guid.NewGuid():N}",
                Description = "Benchmark bucket"
            });
            createdIds.Add(bucket.Id);
        });

        // Get
        if (createdIds.Count > 0)
        {
            var targetId = createdIds[0];
            await ctx.MeasureAsync(Category, "Get Bucket", async () =>
            {
                await ctx.Client.Buckets[targetId].GetAsync();
            });
        }

        // List
        await ctx.MeasureAsync(Category, "List Buckets", async () =>
        {
            await ctx.Client.Buckets.ListAsync();
        });

        // List with pagination
        await ctx.MeasureAsync(Category, "List Buckets (paginated)", async () =>
        {
            await ctx.Client.Buckets.ListAsync(new PaginationOptions { Limit = 5, Offset = 0 });
        });

        // Update
        if (createdIds.Count > 0)
        {
            var targetId = createdIds[0];
            await ctx.MeasureAsync(Category, "Update Bucket", async () =>
            {
                await ctx.Client.Buckets[targetId].UpdateAsync(new UpdateBucketRequest
                {
                    Description = $"Updated {DateTime.UtcNow:O}"
                });
            });
        }

        // Summary
        if (createdIds.Count > 0)
        {
            var targetId = createdIds[0];
            await ctx.MeasureAsync(Category, "Get Summary", async () =>
            {
                await ctx.Client.Buckets[targetId].GetSummaryAsync();
            });
        }

        // Download ZIP (on a bucket with at least one file)
        if (createdIds.Count > 0)
        {
            var targetId = createdIds[0];
            var data = new byte[1024];
            Random.Shared.NextBytes(data);
            await ctx.Client.Buckets[targetId].Files.UploadAsync(data, "bench.bin");

            await ctx.MeasureAsync(Category, "Download ZIP", async () =>
            {
                var stream = await ctx.Client.Buckets[targetId].DownloadZipAsync();
                await stream.DisposeAsync();
            });
        }

        // Delete
        foreach (var id in createdIds)
        {
            await ctx.MeasureOnceAsync(Category, "Delete Bucket", async () =>
            {
                await ctx.Client.Buckets[id].DeleteAsync();
            });
        }
    }
}

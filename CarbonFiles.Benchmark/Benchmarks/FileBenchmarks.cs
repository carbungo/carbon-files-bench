using CarbonFiles.Client.Models;

namespace CarbonFiles.Benchmark.Benchmarks;

public static class FileBenchmarks
{
    private const string Category = "Files";

    public static async Task RunAsync(BenchmarkContext ctx)
    {
        var bucket = await ctx.Client.Buckets.CreateAsync(new CreateBucketRequest
        {
            Name = $"bench-files-{Guid.NewGuid():N}"
        });
        var bucketId = bucket.Id;
        var files = ctx.Client.Buckets[bucketId].Files;

        try
        {
            // Small file upload (1 KB)
            var small = new byte[1024];
            Random.Shared.NextBytes(small);
            await ctx.MeasureThroughputAsync(Category, "Upload 1 KB", async () =>
            {
                await files.UploadAsync(small, $"small-{Guid.NewGuid():N}.bin");
                return small.Length;
            });

            // Medium file upload (1 MB)
            var medium = new byte[1024 * 1024];
            Random.Shared.NextBytes(medium);
            await ctx.MeasureThroughputAsync(Category, "Upload 1 MB", async () =>
            {
                await files.UploadAsync(medium, $"medium-{Guid.NewGuid():N}.bin");
                return medium.Length;
            });

            // Large file upload (10 MB)
            var large = new byte[10 * 1024 * 1024];
            Random.Shared.NextBytes(large);
            await ctx.MeasureThroughputAsync(Category, "Upload 10 MB", async () =>
            {
                await files.UploadAsync(large, $"large-{Guid.NewGuid():N}.bin");
                return large.Length;
            }, iterations: 3);

            // Stream upload (1 MB)
            await ctx.MeasureThroughputAsync(Category, "Stream Upload 1 MB", async () =>
            {
                var ms = new MemoryStream(medium);
                await files.UploadAsync(ms, $"stream-{Guid.NewGuid():N}.bin");
                return medium.Length;
            });

            // Upload a known file for download/metadata tests
            var knownData = new byte[64 * 1024]; // 64 KB
            Random.Shared.NextBytes(knownData);
            var knownName = "known-file.bin";
            await files.UploadAsync(knownData, knownName);

            var fileRes = files[knownName];

            // Download
            await ctx.MeasureThroughputAsync(Category, "Download 64 KB", async () =>
            {
                var stream = await fileRes.DownloadAsync();
                var ms = new MemoryStream();
                await stream.CopyToAsync(ms);
                await stream.DisposeAsync();
                return ms.Length;
            });

            // Get metadata
            await ctx.MeasureAsync(Category, "Get File Metadata", async () =>
            {
                await fileRes.GetMetadataAsync();
            });

            // Verify integrity
            await ctx.MeasureAsync(Category, "Verify File", async () =>
            {
                await fileRes.VerifyAsync();
            });

            // List files
            await ctx.MeasureAsync(Category, "List Files", async () =>
            {
                await files.ListAsync();
            });

            // List tree
            await ctx.MeasureAsync(Category, "List File Tree", async () =>
            {
                await files.ListTreeAsync();
            });

            // List directory
            await ctx.MeasureAsync(Category, "List Directory", async () =>
            {
                await files.ListDirectoryAsync();
            });

            // Append to file
            var appendData = new byte[512];
            Random.Shared.NextBytes(appendData);
            await ctx.MeasureAsync(Category, "Append to File", async () =>
            {
                var ms = new MemoryStream(appendData);
                await fileRes.AppendAsync(ms);
            });

            // Patch file content (range-based)
            var patchData = new byte[1024];
            Random.Shared.NextBytes(patchData);
            await ctx.MeasureAsync(Category, "Patch File", async () =>
            {
                var ms = new MemoryStream(patchData);
                await fileRes.PatchAsync(ms, 0, patchData.Length - 1, patchData.Length);
            });

            // Delete file
            await ctx.MeasureOnceAsync(Category, "Delete File", async () =>
            {
                await fileRes.DeleteAsync();
            });
        }
        finally
        {
            await ctx.Client.Buckets[bucketId].DeleteAsync();
        }
    }
}

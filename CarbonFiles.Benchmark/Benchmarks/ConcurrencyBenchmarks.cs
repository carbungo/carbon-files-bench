using System.Diagnostics;
using CarbonFiles.Client.Models;

namespace CarbonFiles.Benchmark.Benchmarks;

public static class ConcurrencyBenchmarks
{
    private const string Category = "Concurrency";
    private const int ConcurrentCount = 10;

    public static async Task RunAsync(BenchmarkContext ctx)
    {
        var bucket = await ctx.Client.Buckets.CreateAsync(new CreateBucketRequest
        {
            Name = $"bench-conc-{Guid.NewGuid():N}"
        });
        var bucketFiles = ctx.Client.Buckets[bucket.Id].Files;

        try
        {
            var data = new byte[64 * 1024]; // 64 KB per file
            Random.Shared.NextBytes(data);

            // Parallel uploads
            await RunConcurrentAsync(ctx, "Parallel Uploads (10x64KB)", ConcurrentCount, async i =>
            {
                await bucketFiles.UploadAsync(data, $"conc-{i}-{Guid.NewGuid():N}.bin");
            });

            // List files to get names for download
            var listed = await bucketFiles.ListAsync();
            var fileNames = listed.Items.Take(ConcurrentCount).Select(f => f.Name).ToList();

            // Parallel downloads
            if (fileNames.Count > 0)
            {
                await RunConcurrentAsync(ctx, "Parallel Downloads (10x64KB)", fileNames.Count, async i =>
                {
                    var name = fileNames[i % fileNames.Count];
                    var stream = await bucketFiles[name].DownloadAsync();
                    var ms = new MemoryStream();
                    await stream.CopyToAsync(ms);
                    await stream.DisposeAsync();
                });
            }

            // Mixed read/write workload
            await RunConcurrentAsync(ctx, "Mixed Read/Write (10 ops)", ConcurrentCount, async i =>
            {
                if (i % 2 == 0)
                {
                    await bucketFiles.UploadAsync(data, $"mixed-{i}-{Guid.NewGuid():N}.bin");
                }
                else if (fileNames.Count > 0)
                {
                    var name = fileNames[i % fileNames.Count];
                    var stream = await bucketFiles[name].DownloadAsync();
                    var ms = new MemoryStream();
                    await stream.CopyToAsync(ms);
                    await stream.DisposeAsync();
                }
            });

            // Burst bucket creation
            var burstIds = new List<string>();
            await RunConcurrentAsync(ctx, "Burst Bucket Create (10)", ConcurrentCount, async i =>
            {
                var b = await ctx.Client.Buckets.CreateAsync(new CreateBucketRequest
                {
                    Name = $"burst-{i}-{Guid.NewGuid():N}"
                });
                lock (burstIds) burstIds.Add(b.Id);
            });

            // Cleanup burst buckets
            foreach (var id in burstIds)
            {
                try { await ctx.Client.Buckets[id].DeleteAsync(); } catch { }
            }
        }
        finally
        {
            await ctx.Client.Buckets[bucket.Id].DeleteAsync();
        }
    }

    private static async Task RunConcurrentAsync(
        BenchmarkContext ctx, string operation, int count, Func<int, Task> action)
    {
        var result = new BenchmarkResult { Category = Category, Operation = operation };

        try
        {
            var sw = Stopwatch.StartNew();
            var tasks = Enumerable.Range(0, count).Select(async i =>
            {
                var taskSw = Stopwatch.StartNew();
                await action(i);
                taskSw.Stop();
                return taskSw.Elapsed.TotalMilliseconds;
            }).ToList();

            var latencies = await Task.WhenAll(tasks);
            sw.Stop();

            result.LatenciesMs.AddRange(latencies);
            result.LatenciesMs.Sort();

            var wallResult = new BenchmarkResult
            {
                Category = Category,
                Operation = $"{operation} [wall clock]"
            };
            wallResult.LatenciesMs.Add(sw.Elapsed.TotalMilliseconds);
            ctx.Results.Add(wallResult);
        }
        catch (Exception ex)
        {
            result.Success = false;
            result.Error = ex.Message;
        }

        ctx.Results.Add(result);
    }
}

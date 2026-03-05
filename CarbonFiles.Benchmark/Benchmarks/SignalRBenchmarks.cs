using System.Diagnostics;
using CarbonFiles.Client.Models;

namespace CarbonFiles.Benchmark.Benchmarks;

public static class SignalRBenchmarks
{
    private const string Category = "SignalR Events";

    public static async Task RunAsync(BenchmarkContext ctx)
    {
        var bucket = await ctx.Client.Buckets.CreateAsync(new CreateBucketRequest
        {
            Name = $"bench-signalr-{Guid.NewGuid():N}"
        });

        try
        {
            var events = ctx.Client.Events;

            // Connect
            await ctx.MeasureOnceAsync(Category, "Connect to Hub", async () =>
            {
                await events.ConnectAsync();
            });

            // Subscribe to bucket
            await ctx.MeasureOnceAsync(Category, "Subscribe to Bucket", async () =>
            {
                await events.SubscribeToBucketAsync(bucket.Id);
            });

            // Measure event delivery latency
            var eventResult = new BenchmarkResult { Category = Category, Operation = "Event Delivery Latency" };

            try
            {
                for (var i = 0; i < 3; i++)
                {
                    var tcs = new TaskCompletionSource<bool>();
                    var sw = Stopwatch.StartNew();

                    using var sub = events.OnFileCreated((bucketId, file) =>
                    {
                        sw.Stop();
                        tcs.TrySetResult(true);
                        return Task.CompletedTask;
                    });

                    var data = new byte[256];
                    Random.Shared.NextBytes(data);
                    await ctx.Client.Buckets[bucket.Id].Files
                        .UploadAsync(data, $"event-{i}.bin");

                    var completed = await Task.WhenAny(tcs.Task, Task.Delay(5000));

                    if (completed == tcs.Task)
                    {
                        eventResult.LatenciesMs.Add(sw.Elapsed.TotalMilliseconds);
                    }
                    else
                    {
                        eventResult.LatenciesMs.Add(-1); // Timeout marker
                    }
                }

                eventResult.LatenciesMs.Sort();
            }
            catch (Exception ex)
            {
                eventResult.Success = false;
                eventResult.Error = ex.Message;
            }

            ctx.Results.Add(eventResult);

            // Unsubscribe
            await ctx.MeasureOnceAsync(Category, "Unsubscribe from Bucket", async () =>
            {
                await events.UnsubscribeFromBucketAsync(bucket.Id);
            });

            // Disconnect
            await ctx.MeasureOnceAsync(Category, "Disconnect from Hub", async () =>
            {
                await events.DisconnectAsync();
            });
        }
        finally
        {
            await ctx.Client.Buckets[bucket.Id].DeleteAsync();
        }
    }
}

using CarbonFiles.Client;

namespace CarbonFiles.Benchmark;

public sealed class BenchmarkContext
{
    public required CarbonFilesClient Client { get; init; }
    public required string BaseUrl { get; init; }
    public required string AdminKey { get; init; }
    public int Iterations { get; init; } = 5;
    public int MaxUploadMb { get; init; } = 100;
    public List<BenchmarkResult> Results { get; } = [];

    public async Task<BenchmarkResult> MeasureAsync(
        string category, string operation, Func<Task> action, int? iterations = null)
    {
        var result = new BenchmarkResult { Category = category, Operation = operation };
        var count = iterations ?? Iterations;

        try
        {
            // Warmup
            await action();

            for (var i = 0; i < count; i++)
            {
                var sw = System.Diagnostics.Stopwatch.StartNew();
                await action();
                sw.Stop();
                result.LatenciesMs.Add(sw.Elapsed.TotalMilliseconds);
            }

            result.LatenciesMs.Sort();
        }
        catch (Exception ex)
        {
            result.Success = false;
            result.Error = ex.Message;
        }

        Results.Add(result);
        return result;
    }

    public async Task<BenchmarkResult> MeasureOnceAsync(
        string category, string operation, Func<Task> action)
    {
        var result = new BenchmarkResult { Category = category, Operation = operation };

        try
        {
            var sw = System.Diagnostics.Stopwatch.StartNew();
            await action();
            sw.Stop();
            result.LatenciesMs.Add(sw.Elapsed.TotalMilliseconds);
            result.LatenciesMs.Sort();
        }
        catch (Exception ex)
        {
            result.Success = false;
            result.Error = ex.Message;
        }

        Results.Add(result);
        return result;
    }

    public async Task<BenchmarkResult> MeasureThroughputAsync(
        string category, string operation, Func<Task<long>> action, int? iterations = null)
    {
        var result = new BenchmarkResult { Category = category, Operation = operation };
        var count = iterations ?? Iterations;
        long totalBytes = 0;

        try
        {
            // Warmup
            await action();

            for (var i = 0; i < count; i++)
            {
                var sw = System.Diagnostics.Stopwatch.StartNew();
                var bytes = await action();
                sw.Stop();
                result.LatenciesMs.Add(sw.Elapsed.TotalMilliseconds);
                totalBytes += bytes;
            }

            result.LatenciesMs.Sort();
            result.BytesTransferred = totalBytes;
            var totalSeconds = result.LatenciesMs.Sum() / 1000.0;
            if (totalSeconds > 0)
                result.ThroughputMbPerSec = (totalBytes / (1024.0 * 1024.0)) / totalSeconds;
        }
        catch (Exception ex)
        {
            result.Success = false;
            result.Error = ex.Message;
        }

        Results.Add(result);
        return result;
    }
}

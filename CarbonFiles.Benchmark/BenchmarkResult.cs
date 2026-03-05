namespace CarbonFiles.Benchmark;

public sealed class BenchmarkResult
{
    public required string Category { get; init; }
    public required string Operation { get; init; }
    public List<double> LatenciesMs { get; } = [];
    public bool Success { get; set; } = true;
    public string? Error { get; set; }
    public double? ThroughputMbPerSec { get; set; }
    public long? BytesTransferred { get; set; }
}

namespace CarbonFiles.Benchmark;

public static class Statistics
{
    public static double Percentile(List<double> sorted, double p)
    {
        if (sorted.Count == 0) return 0;
        if (sorted.Count == 1) return sorted[0];

        var n = (p / 100.0) * (sorted.Count - 1);
        var lower = (int)Math.Floor(n);
        var upper = (int)Math.Ceiling(n);
        if (lower == upper) return sorted[lower];
        return sorted[lower] + (n - lower) * (sorted[upper] - sorted[lower]);
    }

    public static double Min(List<double> values) => values.Count == 0 ? 0 : values.Min();
    public static double Max(List<double> values) => values.Count == 0 ? 0 : values.Max();
    public static double Median(List<double> values) => Percentile(values, 50);
    public static double P95(List<double> values) => Percentile(values, 95);
    public static double P99(List<double> values) => Percentile(values, 99);
    public static double Mean(List<double> values) => values.Count == 0 ? 0 : values.Average();

    public static double OpsPerSec(List<double> latencies)
    {
        if (latencies.Count == 0) return 0;
        var avgMs = latencies.Average();
        return avgMs > 0 ? 1000.0 / avgMs : 0;
    }
}

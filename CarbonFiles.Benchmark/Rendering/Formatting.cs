namespace CarbonFiles.Benchmark.Rendering;

public static class Formatting
{
    public static string FormatMs(double ms) => ms switch
    {
        < 1 => $"{ms:F3} ms",
        < 1000 => $"{ms:F1} ms",
        _ => $"{ms / 1000.0:F2} s"
    };

    public static string FormatOps(double ops) => ops switch
    {
        >= 1000 => $"{ops:F0}",
        >= 1 => $"{ops:F1}",
        _ => $"{ops:F3}"
    };

    public static string FormatThroughput(double? mbps) =>
        mbps.HasValue ? $"{mbps.Value:F2} MB/s" : "-";
}

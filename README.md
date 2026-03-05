# CarbonFiles Benchmark Suite

A thorough API benchmarking suite for [CarbonFiles](https://github.com/carbungo/carbon-files) using the official [CarbonFiles.Client](https://www.nuget.org/packages/CarbonFiles.Client) SDK and [Spectre.Console](https://spectreconsole.net/) for formatted output.

## Requirements

- .NET 10 SDK
- A running CarbonFiles instance
- An admin API key

## Usage

```bash
cd CarbonFiles.Benchmark

# Run all benchmarks
dotnet run -- --url https://your-server.com --key your-admin-key

# Customize iterations per operation
dotnet run -- --url https://your-server.com --key your-key --iterations 10

# Run a single category
dotnet run -- --url https://your-server.com --key your-key --category Files

# Skip SignalR (useful if the server is behind a reverse proxy that doesn't support WebSockets)
dotnet run -- --url https://your-server.com --key your-key --skip-signalr

# Test large file transfers up to 500 MB
dotnet run -- --url https://your-server.com --key your-key --max-upload-mb 500

# Test large transfers only
dotnet run -- --url https://your-server.com --key your-key --category "Large Transfers" --max-upload-mb 250
```

## Benchmark Categories

| Category | Operations | What it measures |
|---|---|---|
| **Health** | Health check | Baseline API latency |
| **Buckets** | Create, Get, List, List (paginated), Update, Summary, Download ZIP, Delete | Full bucket lifecycle |
| **Files** | Upload 1KB / 1MB / 10MB, Stream upload, Download, Metadata, Verify, List, ListTree, ListDirectory, Patch, Append, Delete | Upload/download throughput and file management |
| **Large Transfers** | Upload, Download, and Stream upload at each size tier (1, 10, 50, 100, 250, 500, 1024 MB) up to `--max-upload-mb` | Sustained throughput for large files |
| **API Keys** | Create, List, Get Usage, Revoke | Key management overhead |
| **Upload Tokens** | Create token, Upload with token | Token-scoped upload flow |
| **Short URLs** | Resolve, Delete | Short URL redirect performance |
| **Dashboard** | Create token, Get current user | Dashboard auth round-trip |
| **Stats** | Get system stats | Stats aggregation cost |
| **Concurrency** | 10x parallel uploads, 10x parallel downloads, mixed read/write, burst bucket creation | Throughput under load |
| **SignalR Events** | Connect, Subscribe, Event delivery latency, Unsubscribe, Disconnect | Real-time notification performance |

## Output

Each operation reports:

- **Min / Median / P95 / P99 / Max** latency
- **Ops/s** (operations per second)
- **Throughput** (MB/s for upload/download operations)
- **Status** (PASS / FAIL)

A final scorecard shows global percentiles and lists any failures.

The process exits with code `0` if all benchmarks pass, `1` if any fail.

## Project Structure

```
CarbonFiles.Benchmark/
├── Program.cs                  # CLI entry point (Spectre.Console.Cli)
├── BenchmarkContext.cs         # Shared context with measurement helpers
├── BenchmarkResult.cs          # Result model
├── Statistics.cs               # Percentile calculations
├── Benchmarks/
│   ├── HealthBenchmarks.cs
│   ├── BucketBenchmarks.cs
│   ├── FileBenchmarks.cs
│   ├── ApiKeyBenchmarks.cs
│   ├── UploadTokenBenchmarks.cs
│   ├── ShortUrlBenchmarks.cs
│   ├── DashboardBenchmarks.cs
│   ├── StatsBenchmarks.cs
│   ├── LargeTransferBenchmarks.cs
│   ├── ConcurrencyBenchmarks.cs
│   └── SignalRBenchmarks.cs
└── Rendering/
    └── SpectreRenderer.cs      # Spectre.Console table rendering
```

using System;
using System.Diagnostics;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;

namespace LaunchDeck.Companion;

internal sealed class FocusResult
{
    public bool Success { get; init; }
    public string Reason { get; init; } = "";
    public int Attempts { get; init; }
    public int ElapsedMs { get; init; }
    public string[] CandidateProcessNames { get; init; } = Array.Empty<string>();
    public int? LaunchedProcessId { get; init; }
    public string? LaunchedProcessName { get; init; }
    public string? TargetWindow { get; init; }
    public int? ForegroundProcessId { get; init; }
    public string? ForegroundProcessName { get; init; }
    public string? ForegroundWindow { get; init; }
    public string[] Events { get; init; } = Array.Empty<string>();
}

internal sealed class ForegroundObservation
{
    public string HWnd { get; init; } = "";
    public int? ProcessId { get; init; }
    public string? ProcessName { get; init; }
    public string Description { get; init; } = "";
}

internal sealed class LaunchDiagnosticOptions
{
    public string Type { get; init; } = "exe";
    public string Path { get; init; } = "";
    public string? Args { get; init; }
    public bool Focus { get; init; }
    public int FocusDelayMs { get; init; } = 300;
    public int TimeoutMs { get; init; } = 7000;
    public int PostFocusObserveMs { get; init; } = 300;
    public int? SimulateFocusStealAfterMs { get; init; }
    public int SimulateFocusStealDurationMs { get; init; } = 3000;
    public string LaunchId { get; init; } = Guid.NewGuid().ToString("N");
    public string ReportPath { get; init; } = GetDefaultReportPath();

    internal static bool TryParse(string[] args, out LaunchDiagnosticOptions options, out string error)
    {
        var type = "exe";
        string? path = null;
        string? launchArgs = null;
        var focus = false;
        var focusDelayMs = 300;
        var timeoutMs = 7000;
        var postFocusObserveMs = 300;
        int? simulateFocusStealAfterMs = null;
        var simulateFocusStealDurationMs = 3000;
        var launchId = Guid.NewGuid().ToString("N");
        string? reportPath = null;

        for (int i = 0; i < args.Length; i++)
        {
            var arg = args[i];
            if (string.Equals(arg, "--diagnose-launch", StringComparison.OrdinalIgnoreCase))
                continue;

            switch (arg.ToLowerInvariant())
            {
                case "--type":
                    type = RequireValue(args, ref i, arg);
                    break;
                case "--path":
                    path = RequireValue(args, ref i, arg);
                    break;
                case "--args":
                    launchArgs = RequireValue(args, ref i, arg);
                    break;
                case "--focus":
                    focus = true;
                    if (i + 1 < args.Length && !args[i + 1].StartsWith("--", StringComparison.Ordinal))
                    {
                        if (!bool.TryParse(args[++i], out focus))
                            throw new ArgumentException("--focus expects true or false when a value is supplied.");
                    }
                    break;
                case "--no-focus":
                    focus = false;
                    break;
                case "--focus-delay-ms":
                    focusDelayMs = ParseNonNegativeInt(RequireValue(args, ref i, arg), arg);
                    break;
                case "--timeout-ms":
                    timeoutMs = ParsePositiveInt(RequireValue(args, ref i, arg), arg);
                    break;
                case "--post-focus-observe-ms":
                    postFocusObserveMs = ParseNonNegativeInt(RequireValue(args, ref i, arg), arg);
                    break;
                case "--simulate-focus-steal-after-ms":
                    simulateFocusStealAfterMs = ParseNonNegativeInt(RequireValue(args, ref i, arg), arg);
                    break;
                case "--simulate-focus-steal-duration-ms":
                    simulateFocusStealDurationMs = ParsePositiveInt(RequireValue(args, ref i, arg), arg);
                    break;
                case "--launch-id":
                    launchId = RequireValue(args, ref i, arg);
                    break;
                case "--report":
                    reportPath = RequireValue(args, ref i, arg);
                    break;
                default:
                    error = $"Unknown diagnostic option: {arg}";
                    options = new LaunchDiagnosticOptions();
                    return false;
            }
        }

        if (string.IsNullOrWhiteSpace(path))
        {
            error = "--path is required.";
            options = new LaunchDiagnosticOptions();
            return false;
        }

        options = new LaunchDiagnosticOptions
        {
            Type = type,
            Path = path,
            Args = string.IsNullOrWhiteSpace(launchArgs) ? null : launchArgs,
            Focus = focus,
            FocusDelayMs = focusDelayMs,
            TimeoutMs = timeoutMs,
            PostFocusObserveMs = postFocusObserveMs,
            SimulateFocusStealAfterMs = simulateFocusStealAfterMs,
            SimulateFocusStealDurationMs = simulateFocusStealDurationMs,
            LaunchId = string.IsNullOrWhiteSpace(launchId) ? Guid.NewGuid().ToString("N") : launchId,
            ReportPath = string.IsNullOrWhiteSpace(reportPath) ? GetDefaultReportPath() : reportPath
        };
        error = "";
        return true;
    }

    private static string RequireValue(string[] args, ref int index, string option)
    {
        if (index + 1 >= args.Length)
            throw new ArgumentException($"{option} requires a value.");
        return args[++index];
    }

    private static int ParsePositiveInt(string value, string option)
    {
        if (!int.TryParse(value, out var parsed) || parsed <= 0)
            throw new ArgumentException($"{option} requires a positive integer.");
        return parsed;
    }

    private static int ParseNonNegativeInt(string value, string option)
    {
        if (!int.TryParse(value, out var parsed) || parsed < 0)
            throw new ArgumentException($"{option} requires a non-negative integer.");
        return parsed;
    }

    private static string GetDefaultReportPath()
    {
        var dir = System.IO.Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "LaunchDeck",
            "Diagnostics");
        var fileName = $"launch-focus-{DateTime.Now:yyyyMMdd-HHmmss-fff}.json";
        return System.IO.Path.Combine(dir, fileName);
    }
}

internal sealed class LaunchDiagnosticReport
{
    public string LaunchId { get; init; } = "";
    public string Type { get; init; } = "";
    public string Path { get; init; } = "";
    public string? Args { get; init; }
    public bool FocusRequested { get; init; }
    public int FocusDelayMs { get; init; }
    public int TimeoutMs { get; init; }
    public int PostFocusObserveMs { get; init; }
    public int? SimulateFocusStealAfterMs { get; init; }
    public int SimulateFocusStealDurationMs { get; init; }
    public bool LaunchSuccess { get; set; }
    public string? Error { get; set; }
    public int? ProcessId { get; set; }
    public string? ProcessName { get; set; }
    public int ElapsedMs { get; set; }
    public string CompanionLogPath { get; init; } = Log.Path;
    public FocusResult? Focus { get; set; }
    public bool? FocusRetained { get; set; }
    public ForegroundObservation? FinalForeground { get; set; }
}

internal static class LaunchDiagnostics
{
    internal static async Task<int> RunLaunchDiagnosticAsync(string[] args)
    {
        LaunchDiagnosticOptions options;
        try
        {
            if (!LaunchDiagnosticOptions.TryParse(args, out options, out var error))
            {
                Log.Write($"diagnostic: invalid args: {error}");
                return 64;
            }
        }
        catch (Exception ex)
        {
            Log.Write($"diagnostic: invalid args: {ex.Message}");
            return 64;
        }

        var report = new LaunchDiagnosticReport
        {
            LaunchId = options.LaunchId,
            Type = options.Type,
            Path = options.Path,
            Args = options.Args,
            FocusRequested = options.Focus,
            FocusDelayMs = options.FocusDelayMs,
            TimeoutMs = options.TimeoutMs,
            PostFocusObserveMs = options.PostFocusObserveMs,
            SimulateFocusStealAfterMs = options.SimulateFocusStealAfterMs,
            SimulateFocusStealDurationMs = options.SimulateFocusStealDurationMs
        };

        var elapsed = Stopwatch.StartNew();
        Log.Write($"diagnostic[{options.LaunchId}]: launch type={options.Type} path={options.Path} focus={options.Focus} delayMs={options.FocusDelayMs} timeoutMs={options.TimeoutMs}");

        FocusStealSimulator? focusStealSimulator = null;
        var (success, launchError, process) = LaunchHandler.Launch(options.Type, options.Path, options.Args);
        report.LaunchSuccess = success;
        report.Error = launchError;
        report.ProcessId = TryGetProcessId(process);
        report.ProcessName = TryGetProcessName(process);

        Log.Write($"diagnostic[{options.LaunchId}]: launch result success={success} error={launchError ?? ""} pid={report.ProcessId?.ToString() ?? ""} process={report.ProcessName ?? ""}");

        if (success && options.SimulateFocusStealAfterMs.HasValue)
        {
            focusStealSimulator = FocusStealSimulator.Start(
                options.SimulateFocusStealAfterMs.Value,
                options.SimulateFocusStealDurationMs);
            Log.Write($"diagnostic[{options.LaunchId}]: focus-steal simulation scheduled afterMs={options.SimulateFocusStealAfterMs.Value} durationMs={options.SimulateFocusStealDurationMs}");
        }

        if (success && process != null && options.Focus)
        {
            if (options.FocusDelayMs > 0)
                await Task.Delay(options.FocusDelayMs);

            report.Focus = await NativeMethods.FocusProcessAsync(
                process,
                options.Path,
                timeoutMs: options.TimeoutMs);
        }

        if (options.PostFocusObserveMs > 0)
            await Task.Delay(options.PostFocusObserveMs);

        report.FinalForeground = NativeMethods.GetForegroundObservation();
        if (options.Focus)
        {
            var expectedProcessNames = report.Focus?.CandidateProcessNames ?? Array.Empty<string>();
            report.FocusRetained = expectedProcessNames.Length > 0 &&
                                   NativeMethods.IsForegroundProcessExpected(expectedProcessNames);
        }

        report.ElapsedMs = (int)elapsed.ElapsedMilliseconds;
        WriteReport(options.ReportPath, report);

        focusStealSimulator?.Dispose();

        Log.Write($"diagnostic[{options.LaunchId}]: complete launchSuccess={report.LaunchSuccess} focusSuccess={report.Focus?.Success.ToString() ?? ""} focusRetained={report.FocusRetained?.ToString() ?? ""} finalForeground={report.FinalForeground?.Description ?? ""} report={options.ReportPath}");

        return report.LaunchSuccess && (!options.Focus || report.Focus?.Success == true)
                                   && (!options.Focus || report.FocusRetained == true)
            ? 0
            : 1;
    }

    private static void WriteReport(string path, LaunchDiagnosticReport report)
    {
        var dir = System.IO.Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(dir))
            Directory.CreateDirectory(dir);

        var json = JsonSerializer.Serialize(report, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(path, json);
    }

    private static int? TryGetProcessId(Process? process)
    {
        if (process == null)
            return null;

        try { return process.Id; }
        catch { return null; }
    }

    private static string? TryGetProcessName(Process? process)
    {
        if (process == null)
            return null;

        try { return process.ProcessName; }
        catch { return null; }
    }
}

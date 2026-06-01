using LaunchDeck.Companion;
using Xunit;

namespace LaunchDeck.Tests;

public class LaunchDiagnosticsTests
{
    [Fact]
    public void TryParse_WithFocusOptions_ParsesLaunchDiagnostic()
    {
        var parsed = LaunchDiagnosticOptions.TryParse(
            new[]
            {
                "--diagnose-launch",
                "--type", "exe",
                "--path", @"C:\Program Files\Google\Chrome\Application\chrome.exe",
                "--args", "--new-window",
                "--focus",
                "--focus-delay-ms", "350",
                "--timeout-ms", "7000",
                "--post-focus-observe-ms", "900",
                "--simulate-focus-steal-after-ms", "150",
                "--simulate-focus-steal-duration-ms", "3000",
                "--launch-id", "test-launch",
                "--report", @"C:\Temp\launch-report.json"
            },
            out var options,
            out var error);

        Assert.True(parsed, error);
        Assert.Equal("exe", options.Type);
        Assert.Equal(@"C:\Program Files\Google\Chrome\Application\chrome.exe", options.Path);
        Assert.Equal("--new-window", options.Args);
        Assert.True(options.Focus);
        Assert.Equal(350, options.FocusDelayMs);
        Assert.Equal(7000, options.TimeoutMs);
        Assert.Equal(900, options.PostFocusObserveMs);
        Assert.Equal(150, options.SimulateFocusStealAfterMs);
        Assert.Equal(3000, options.SimulateFocusStealDurationMs);
        Assert.Equal("test-launch", options.LaunchId);
        Assert.Equal(@"C:\Temp\launch-report.json", options.ReportPath);
    }

    [Fact]
    public void TryParse_MissingPath_ReturnsFalse()
    {
        var parsed = LaunchDiagnosticOptions.TryParse(
            new[] { "--diagnose-launch", "--type", "exe" },
            out _,
            out var error);

        Assert.False(parsed);
        Assert.Equal("--path is required.", error);
    }

    [Fact]
    public void TryParse_NoFocus_DisablesFocus()
    {
        var parsed = LaunchDiagnosticOptions.TryParse(
            new[] { "--diagnose-launch", "--path", "notepad.exe", "--focus", "true", "--no-focus" },
            out var options,
            out var error);

        Assert.True(parsed, error);
        Assert.False(options.Focus);
    }
}

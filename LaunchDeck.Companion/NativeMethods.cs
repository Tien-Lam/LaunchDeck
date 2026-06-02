using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace LaunchDeck.Companion;

internal static class NativeMethods
{
    private delegate bool EnumWindowsProc(nint hWnd, nint lParam);

    [DllImport("user32.dll")]
    private static extern bool EnumWindows(EnumWindowsProc lpEnumFunc, nint lParam);

    [DllImport("user32.dll")]
    private static extern bool EnumChildWindows(nint hWndParent, EnumWindowsProc lpEnumFunc, nint lParam);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool SetForegroundWindow(nint hWnd);

    [DllImport("user32.dll")]
    private static extern bool ShowWindow(nint hWnd, int nCmdShow);

    [DllImport("user32.dll")]
    private static extern bool BringWindowToTop(nint hWnd);

    [DllImport("user32.dll")]
    private static extern nint SetFocus(nint hWnd);

    [DllImport("user32.dll")]
    private static extern bool IsWindowVisible(nint hWnd);

    [DllImport("user32.dll")]
    private static extern int GetWindowTextLength(nint hWnd);

    [DllImport("user32.dll")]
    private static extern uint GetWindowThreadProcessId(nint hWnd, out uint processId);

    [DllImport("user32.dll")]
    private static extern nint GetForegroundWindow();

    [DllImport("kernel32.dll")]
    private static extern uint GetCurrentThreadId();

    [DllImport("user32.dll")]
    private static extern bool AttachThreadInput(uint idAttach, uint idAttachTo, bool fAttach);

    private const int SW_RESTORE = 9;

    internal static ForegroundObservation GetForegroundObservation()
    {
        var snapshot = GetForegroundSnapshot();
        return new ForegroundObservation
        {
            HWnd = FormatHwnd(snapshot.HWnd),
            ProcessId = snapshot.ProcessId,
            ProcessName = snapshot.ProcessName,
            Description = snapshot.Describe()
        };
    }

    internal static bool IsForegroundProcessExpected(string[] expectedProcessNames)
    {
        var foreground = GetForegroundSnapshot();
        return foreground.ProcessName != null &&
               expectedProcessNames.Contains(foreground.ProcessName, StringComparer.OrdinalIgnoreCase) ||
               WindowHasDescendantProcessName(foreground.HWnd, expectedProcessNames);
    }

    internal static async Task<FocusResult> FocusProcessAsync(
        Process process,
        string executablePath,
        int timeoutMs = 5000,
        int intervalMs = 100)
    {
        return await FocusLaunchTargetAsync(
            process,
            "exe",
            executablePath,
            timeoutMs,
            intervalMs);
    }

    internal static async Task<FocusResult> FocusLaunchTargetAsync(
        Process? process,
        string launchType,
        string launchPath,
        int timeoutMs = 5000,
        int intervalMs = 100)
    {
        var stopwatch = Stopwatch.StartNew();
        var processNames = FocusTargetResolver.GetCandidateProcessNames(process, launchType, launchPath);
        var events = new List<string>
        {
            $"candidates={string.Join(",", processNames)}"
        };
        var attempts = 0;
        WindowSnapshot? targetWindow = null;
        var launchedProcessId = TryGetProcessId(process);
        var launchedProcessName = TryGetProcessName(process);

        if (processNames.Length == 0)
        {
            return BuildFocusResult(
                success: false,
                reason: "no focus candidates",
                attempts,
                stopwatch,
                processNames,
                launchedProcessId,
                launchedProcessName,
                targetWindow,
                events);
        }

        while (stopwatch.ElapsedMilliseconds < timeoutMs)
        {
            attempts++;
            await Task.Delay(intervalMs);

            if (process != null)
            {
                var mainWindowResult = await TryFocusProcessMainWindowAsync(process, processNames, events);
                targetWindow = mainWindowResult.TargetWindow ?? targetWindow;
                if (mainWindowResult.Success)
                {
                    return BuildFocusResult(
                        success: true,
                        reason: "focused launched process main window",
                        attempts,
                        stopwatch,
                        processNames,
                        launchedProcessId,
                        launchedProcessName,
                        targetWindow,
                        events);
                }
            }

            var matchingWindowResult = await TryFocusProcessWindowByNameAsync(processNames, events);
            targetWindow = matchingWindowResult.TargetWindow ?? targetWindow;
            if (matchingWindowResult.Success)
            {
                return BuildFocusResult(
                    success: true,
                    reason: "focused matching process window",
                    attempts,
                    stopwatch,
                    processNames,
                    launchedProcessId,
                    launchedProcessName,
                    targetWindow,
                    events);
            }
        }

        return BuildFocusResult(
            success: false,
            reason: $"timed out after {timeoutMs}ms",
            attempts,
            stopwatch,
            processNames,
            launchedProcessId,
            launchedProcessName,
            targetWindow,
            events);
    }

    private static FocusResult BuildFocusResult(
        bool success,
        string reason,
        int attempts,
        Stopwatch stopwatch,
        string[] processNames,
        int? launchedProcessId,
        string? launchedProcessName,
        WindowSnapshot? targetWindow,
        List<string> events)
    {
        var foreground = GetForegroundSnapshot();
        return new FocusResult
        {
            Success = success,
            Reason = reason,
            Attempts = attempts,
            ElapsedMs = (int)stopwatch.ElapsedMilliseconds,
            CandidateProcessNames = processNames,
            LaunchedProcessId = launchedProcessId,
            LaunchedProcessName = launchedProcessName,
            TargetWindow = targetWindow?.Describe(),
            ForegroundProcessId = foreground.ProcessId,
            ForegroundProcessName = foreground.ProcessName,
            ForegroundWindow = foreground.Describe(),
            Events = events.ToArray()
        };
    }

    private static async Task<(bool Success, WindowSnapshot? TargetWindow)> TryFocusProcessMainWindowAsync(
        Process process,
        string[] expectedProcessNames,
        List<string> events)
    {
        try
        {
            process.Refresh();
            var hWnd = process.MainWindowHandle;
            if (hWnd == nint.Zero)
                return (false, null);

            var targetWindow = GetWindowSnapshot(hWnd);
            var success = await TryFocusWindowAsync(hWnd, expectedProcessNames, events, "main-window");
            return (success, targetWindow);
        }
        catch (InvalidOperationException)
        {
            events.Add("main-window: process exited");
            return (false, null);
        }
        catch (System.ComponentModel.Win32Exception ex)
        {
            events.Add($"main-window: access failed {ex.NativeErrorCode}");
            return (false, null);
        }
    }

    private static async Task<(bool Success, WindowSnapshot? TargetWindow)> TryFocusProcessWindowByNameAsync(
        string[] processNames,
        List<string> events)
    {
        WindowSnapshot? targetWindow = null;
        foreach (var processName in processNames)
        {
            Process[] processes;
            try
            {
                processes = Process.GetProcessesByName(processName);
            }
            catch (InvalidOperationException)
            {
                continue;
            }

            foreach (var process in processes.OrderByDescending(GetProcessStartTime))
            {
                using (process)
                {
                    try
                    {
                        process.Refresh();
                        var hWnd = process.MainWindowHandle;
                        if (hWnd == nint.Zero)
                            continue;

                        targetWindow = GetWindowSnapshot(hWnd);
                        if (await TryFocusWindowAsync(hWnd, processNames, events, $"process-name:{processName}"))
                            return (true, targetWindow);
                    }
                    catch (InvalidOperationException) { }
                    catch (System.ComponentModel.Win32Exception ex)
                    {
                        events.Add($"process-name:{processName}: access failed {ex.NativeErrorCode}");
                    }
                }
            }
        }

        foreach (var hWnd in EnumerateCandidateWindows(processNames))
        {
            targetWindow = GetWindowSnapshot(hWnd);
            if (await TryFocusWindowAsync(hWnd, processNames, events, "enum-window"))
                return (true, targetWindow);
        }

        return (false, targetWindow);
    }

    private static DateTime GetProcessStartTime(Process process)
    {
        try { return process.StartTime; }
        catch { return DateTime.MinValue; }
    }

    private static nint[] EnumerateCandidateWindows(string[] processNames)
    {
        var windows = new List<nint>();
        EnumWindows((hWnd, _) =>
        {
            if (!IsWindowVisible(hWnd) || GetWindowTextLength(hWnd) == 0)
                return true;

            GetWindowThreadProcessId(hWnd, out var pid);
            try
            {
                using var process = Process.GetProcessById((int)pid);
                if (processNames.Contains(process.ProcessName, StringComparer.OrdinalIgnoreCase) ||
                    WindowHasDescendantProcessName(hWnd, processNames))
                {
                    windows.Add(hWnd);
                }
            }
            catch
            {
                // Window disappeared or process access was denied.
            }

            return true;
        }, nint.Zero);

        return windows.ToArray();
    }

    private static bool WindowHasDescendantProcessName(nint hWnd, string[] processNames)
    {
        if (hWnd == nint.Zero || processNames.Length == 0)
            return false;

        var found = false;
        try
        {
            EnumChildWindows(hWnd, (childHwnd, _) =>
            {
                GetWindowThreadProcessId(childHwnd, out var childPid);
                if (childPid == 0)
                    return true;

                try
                {
                    using var childProcess = Process.GetProcessById((int)childPid);
                    if (processNames.Contains(childProcess.ProcessName, StringComparer.OrdinalIgnoreCase))
                    {
                        found = true;
                        return false;
                    }
                }
                catch
                {
                }

                return true;
            }, nint.Zero);
        }
        catch
        {
        }

        return found;
    }

    private static async Task<bool> TryFocusWindowAsync(
        nint hWnd,
        string[] expectedProcessNames,
        List<string> events,
        string source)
    {
        if (hWnd == nint.Zero)
            return false;

        var before = GetForegroundSnapshot();
        var target = GetWindowSnapshot(hWnd);
        if (IsExpectedForeground(before, target, expectedProcessNames))
        {
            events.Add($"{source}: already foreground target={target.Describe()} foreground={before.Describe()}");
            return true;
        }

        var foregroundWindow = before.HWnd;
        var currentThreadId = GetCurrentThreadId();
        var targetThreadId = GetWindowThreadProcessId(hWnd, out _);
        var foregroundThreadId = foregroundWindow == nint.Zero
            ? 0
            : GetWindowThreadProcessId(foregroundWindow, out _);

        var attachedToTarget = targetThreadId != 0 && AttachThreadInput(currentThreadId, targetThreadId, true);
        var attachedToForeground = foregroundThreadId != 0 && foregroundThreadId != targetThreadId &&
                                   AttachThreadInput(currentThreadId, foregroundThreadId, true);

        var setForeground = false;
        var lastError = 0;
        try
        {
            ShowWindow(hWnd, SW_RESTORE);
            BringWindowToTop(hWnd);
            SetFocus(hWnd);
            setForeground = SetForegroundWindow(hWnd);
            lastError = Marshal.GetLastWin32Error();
        }
        finally
        {
            if (attachedToForeground)
                AttachThreadInput(currentThreadId, foregroundThreadId, false);
            if (attachedToTarget)
                AttachThreadInput(currentThreadId, targetThreadId, false);
        }

        await Task.Delay(75);
        var after = GetForegroundSnapshot();
        var success = IsExpectedForeground(after, target, expectedProcessNames);
        events.Add($"{source}: target={target.Describe()} before={before.Describe()} setForeground={setForeground} lastError={lastError} after={after.Describe()} success={success}");
        return success;
    }

    private static bool IsExpectedForeground(
        WindowSnapshot foreground,
        WindowSnapshot target,
        string[] expectedProcessNames)
    {
        if (foreground.HWnd != nint.Zero && foreground.HWnd == target.HWnd)
            return true;

        return foreground.ProcessName != null &&
               expectedProcessNames.Contains(foreground.ProcessName, StringComparer.OrdinalIgnoreCase);
    }

    private static WindowSnapshot GetForegroundSnapshot()
    {
        return GetWindowSnapshot(GetForegroundWindow());
    }

    private static WindowSnapshot GetWindowSnapshot(nint hWnd)
    {
        if (hWnd == nint.Zero)
            return new WindowSnapshot(hWnd, null, null);

        GetWindowThreadProcessId(hWnd, out var pid);
        if (pid == 0)
            return new WindowSnapshot(hWnd, null, null);

        try
        {
            using var process = Process.GetProcessById((int)pid);
            return new WindowSnapshot(hWnd, (int)pid, process.ProcessName);
        }
        catch
        {
            return new WindowSnapshot(hWnd, (int)pid, null);
        }
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

    private sealed class WindowSnapshot
    {
        public WindowSnapshot(nint hWnd, int? processId, string? processName)
        {
            HWnd = hWnd;
            ProcessId = processId;
            ProcessName = processName;
        }

        public nint HWnd { get; }
        public int? ProcessId { get; }
        public string? ProcessName { get; }

        public string Describe()
        {
            return $"{FormatHwnd(HWnd)} {ProcessName ?? "unknown"}/{ProcessId?.ToString() ?? "unknown"}";
        }
    }

    private static string FormatHwnd(nint hWnd)
    {
        return $"0x{hWnd.ToInt64():X}";
    }
}

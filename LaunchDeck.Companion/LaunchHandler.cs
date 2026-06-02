using System;
using System.Diagnostics;

namespace LaunchDeck.Companion;

public static class LaunchHandler
{
    private const string StoreAppsFolderPrefix = @"shell:AppsFolder\";

    public static ProcessStartInfo BuildProcessStartInfo(string type, string path, string? args)
    {
        return type.ToLowerInvariant() switch
        {
            "exe" => new ProcessStartInfo
            {
                FileName = path,
                Arguments = args ?? "",
                UseShellExecute = true
            },
            "url" or "store" => new ProcessStartInfo
            {
                FileName = path,
                UseShellExecute = true
            },
            _ => throw new ArgumentException($"Unknown launch type: {type}", nameof(type))
        };
    }

    public static (bool Success, string? Error, Process? Process) Launch(string type, string path, string? args)
    {
        try
        {
            if (string.Equals(type, "store", StringComparison.OrdinalIgnoreCase) &&
                TryExtractStoreAumid(path, out var aumid))
            {
                return StoreAppActivator.Activate(aumid);
            }

            var startInfo = BuildProcessStartInfo(type, path, args);
            var process = Process.Start(startInfo);
            return (true, null, process);
        }
        catch (Exception ex)
        {
            return (false, ex.Message, null);
        }
    }

    internal static bool TryExtractStoreAumid(string path, out string aumid)
    {
        if (path.StartsWith(StoreAppsFolderPrefix, StringComparison.OrdinalIgnoreCase))
        {
            aumid = path.Substring(StoreAppsFolderPrefix.Length);
            return !string.IsNullOrWhiteSpace(aumid);
        }

        if (path.Contains('!') && !path.Contains('\\') && !path.Contains('/'))
        {
            aumid = path;
            return !string.IsNullOrWhiteSpace(aumid);
        }

        aumid = "";
        return false;
    }
}

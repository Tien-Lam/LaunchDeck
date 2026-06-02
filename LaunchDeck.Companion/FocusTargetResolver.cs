using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Microsoft.Win32;

namespace LaunchDeck.Companion;

internal static class FocusTargetResolver
{
    internal static string[] GetCandidateProcessNames(Process? process, string launchType, string launchPath)
    {
        var candidates = new List<string?>();

        candidates.Add(TryGetProcessName(process));
        if (string.Equals(Path.GetExtension(launchPath), ".exe", StringComparison.OrdinalIgnoreCase))
            candidates.Add(Path.GetFileNameWithoutExtension(launchPath));

        if (string.Equals(launchType, "url", StringComparison.OrdinalIgnoreCase))
            candidates.Add(TryGetUrlHandlerProcessName(launchPath));

        return candidates
            .Where(name => !string.IsNullOrWhiteSpace(name))
            .Select(name => name!)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    internal static string? ExtractExecutablePathFromCommand(string? command)
    {
        if (string.IsNullOrWhiteSpace(command))
            return null;

        command = Environment.ExpandEnvironmentVariables(command.Trim());
        if (command.Length == 0)
            return null;

        if (command[0] == '"')
        {
            var endQuote = command.IndexOf('"', 1);
            if (endQuote > 1)
                return command.Substring(1, endQuote - 1);
        }

        var exeIndex = command.IndexOf(".exe", StringComparison.OrdinalIgnoreCase);
        if (exeIndex >= 0)
            return command.Substring(0, exeIndex + 4).Trim('"');

        var firstSpace = command.IndexOf(' ');
        return firstSpace > 0 ? command.Substring(0, firstSpace).Trim('"') : command.Trim('"');
    }

    private static string? TryGetUrlHandlerProcessName(string url)
    {
        var command = TryGetUrlHandlerCommand(url);
        var exePath = ExtractExecutablePathFromCommand(command);
        return string.IsNullOrWhiteSpace(exePath)
            ? null
            : Path.GetFileNameWithoutExtension(exePath);
    }

    private static string? TryGetUrlHandlerCommand(string url)
    {
        try
        {
            var scheme = Uri.TryCreate(url, UriKind.Absolute, out var uri)
                ? uri.Scheme
                : "https";

            var progId = Registry.GetValue(
                $@"HKEY_CURRENT_USER\Software\Microsoft\Windows\Shell\Associations\UrlAssociations\{scheme}\UserChoice",
                "ProgId",
                null) as string;

            return TryGetOpenCommand(progId) ?? TryGetOpenCommand(scheme);
        }
        catch
        {
            return null;
        }
    }

    private static string? TryGetOpenCommand(string? progId)
    {
        if (string.IsNullOrWhiteSpace(progId))
            return null;

        return Registry.GetValue($@"HKEY_CURRENT_USER\Software\Classes\{progId}\shell\open\command", null, null) as string
               ?? Registry.GetValue($@"HKEY_CLASSES_ROOT\{progId}\shell\open\command", null, null) as string;
    }

    private static string? TryGetProcessName(Process? process)
    {
        if (process == null)
            return null;

        try { return process.ProcessName; }
        catch { return null; }
    }
}

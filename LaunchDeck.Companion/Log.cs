using System;
using System.IO;

namespace LaunchDeck.Companion;

internal static class Log
{
    private static readonly string LogPath;
    private static readonly object Lock = new();
    private const long MaxSize = 100 * 1024; // 100 KB

    static Log()
    {
        var dir = System.IO.Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "LaunchDeck");
        Directory.CreateDirectory(dir);
        LogPath = System.IO.Path.Combine(dir, "companion.log");

        // Truncate if too large
        try
        {
            if (File.Exists(LogPath) && new FileInfo(LogPath).Length > MaxSize)
                File.WriteAllText(LogPath, "");
        }
        catch { }
    }

    internal static string Path => LogPath;

    internal static void Write(string message)
    {
        lock (Lock)
        {
            try
            {
                File.AppendAllText(LogPath, $"[{DateTime.Now:HH:mm:ss.fff}] {message}{Environment.NewLine}");
            }
            catch { }
        }
    }
}

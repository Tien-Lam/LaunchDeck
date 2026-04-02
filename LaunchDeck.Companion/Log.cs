using System;
using System.IO;

namespace LaunchDeck.Companion;

internal static class Log
{
    private static readonly string LogPath;
    private static readonly object Lock = new();

    static Log()
    {
        var dir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "LaunchDeck");
        Directory.CreateDirectory(dir);
        LogPath = Path.Combine(dir, "companion.log");
    }

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

using System;
using System.IO;

namespace LaunchPad.Widget.Services;

internal static class WidgetLog
{
    private static readonly string LogPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "LaunchPad", "widget.log");

    internal static void Write(string message)
    {
        try
        {
            var dir = Path.GetDirectoryName(LogPath);
            if (dir != null) Directory.CreateDirectory(dir);
            File.AppendAllText(LogPath, $"[{DateTime.Now:HH:mm:ss.fff}] {message}\n");
        }
        catch { }
    }

    internal static void Clear()
    {
        try { File.Delete(LogPath); } catch { }
    }
}

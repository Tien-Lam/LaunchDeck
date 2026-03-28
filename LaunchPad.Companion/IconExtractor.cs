using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace LaunchPad.Companion;

public static class IconExtractor
{
    private static readonly HttpClient HttpClient = new();
    private static readonly string[] SupportedImageExtensions = { ".png", ".jpg", ".jpeg", ".bmp", ".ico" };

    public static string GetCacheFileName(string inputPath)
    {
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(inputPath));
        return Convert.ToHexString(hash)[..16].ToLowerInvariant() + ".png";
    }

    public static string GetIconCacheDir()
    {
        var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        var dir = Path.Combine(localAppData, "LaunchPad", "icons");
        Directory.CreateDirectory(dir);
        return dir;
    }

    public static (bool Success, string? IconPath) ExtractFromExe(string exePath, string cacheDir)
    {
        try
        {
            if (!File.Exists(exePath))
                return (false, null);

            var cacheFile = Path.Combine(cacheDir, GetCacheFileName(exePath));

            if (File.Exists(cacheFile))
            {
                var cacheTime = File.GetLastWriteTimeUtc(cacheFile);
                var exeTime = File.GetLastWriteTimeUtc(exePath);
                if (cacheTime >= exeTime)
                    return (true, cacheFile);
            }

            using var icon = Icon.ExtractAssociatedIcon(exePath);
            if (icon == null)
                return (false, null);

            using var bitmap = icon.ToBitmap();
            bitmap.Save(cacheFile, ImageFormat.Png);
            return (true, cacheFile);
        }
        catch (Exception)
        {
            return (false, null);
        }
    }

    public static string GetFaviconUrl(string url)
    {
        var uri = new Uri(url);
        return $"https://www.google.com/s2/favicons?domain={uri.Host}&sz=64";
    }

    public static async Task<(bool Success, string? IconPath)> FetchFaviconAsync(string url, string cacheDir)
    {
        try
        {
            var cacheFile = Path.Combine(cacheDir, GetCacheFileName(url));
            if (File.Exists(cacheFile))
                return (true, cacheFile);

            var faviconUrl = GetFaviconUrl(url);
            var bytes = await HttpClient.GetByteArrayAsync(faviconUrl);
            await File.WriteAllBytesAsync(cacheFile, bytes);
            return (true, cacheFile);
        }
        catch (Exception)
        {
            return (false, null);
        }
    }

    public static (bool Success, byte[]? Data) LoadCustomIcon(string path)
    {
        try
        {
            if (!File.Exists(path))
                return (false, null);

            var ext = Path.GetExtension(path).ToLowerInvariant();
            if (Array.IndexOf(SupportedImageExtensions, ext) < 0)
                return (false, null);

            if (ext == ".ico")
            {
                using var icon = new Icon(path);
                using var bitmap = icon.ToBitmap();
                using var ms = new MemoryStream();
                bitmap.Save(ms, ImageFormat.Png);
                return (true, ms.ToArray());
            }

            if (ext == ".png")
            {
                return (true, File.ReadAllBytes(path));
            }

            // jpg, jpeg, bmp -- convert to PNG
            using (var bitmap = new Bitmap(path))
            using (var ms = new MemoryStream())
            {
                bitmap.Save(ms, ImageFormat.Png);
                return (true, ms.ToArray());
            }
        }
        catch
        {
            return (false, null);
        }
    }
}

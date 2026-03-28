using System.IO;
using LaunchPad.Companion;

namespace LaunchPad.Tests;

public class IconExtractorCacheTests
{
    [Fact]
    public void GetIconCacheDir_CreatesDirectory()
    {
        var dir = IconExtractor.GetIconCacheDir();

        Assert.True(Directory.Exists(dir));
        Assert.EndsWith("icons", dir);
    }

    [Fact]
    public void ExtractFromExe_CacheHit_ReturnsCachedFile()
    {
        var cacheDir = Path.Combine(Path.GetTempPath(), "launchpad-test-cache");
        Directory.CreateDirectory(cacheDir);

        try
        {
            var result1 = IconExtractor.ExtractFromExe(@"C:\Windows\notepad.exe", cacheDir);
            Assert.True(result1.Success);

            var cacheFile = result1.IconPath!;
            var cacheWriteTime = File.GetLastWriteTimeUtc(cacheFile);

            var result2 = IconExtractor.ExtractFromExe(@"C:\Windows\notepad.exe", cacheDir);
            Assert.True(result2.Success);
            Assert.Equal(cacheFile, result2.IconPath);
            Assert.Equal(cacheWriteTime, File.GetLastWriteTimeUtc(result2.IconPath!));
        }
        finally
        {
            if (Directory.Exists(cacheDir))
                Directory.Delete(cacheDir, true);
        }
    }

    [Fact]
    public void ExtractFromExe_StaleCache_ReExtracts()
    {
        var cacheDir = Path.Combine(Path.GetTempPath(), "launchpad-test-stale");
        Directory.CreateDirectory(cacheDir);

        try
        {
            // First extraction creates cache file
            var result1 = IconExtractor.ExtractFromExe(@"C:\Windows\notepad.exe", cacheDir);
            Assert.True(result1.Success);

            // Set cache file timestamp to the past (older than the EXE)
            File.SetLastWriteTimeUtc(result1.IconPath!, DateTime.UtcNow.AddDays(-365));
            var oldTime = File.GetLastWriteTimeUtc(result1.IconPath!);

            // Second extraction should re-extract (cache is stale)
            var result2 = IconExtractor.ExtractFromExe(@"C:\Windows\notepad.exe", cacheDir);
            Assert.True(result2.Success);

            var newTime = File.GetLastWriteTimeUtc(result2.IconPath!);
            Assert.True(newTime > oldTime, "Cache file should have been rewritten");
        }
        finally
        {
            if (Directory.Exists(cacheDir))
                Directory.Delete(cacheDir, true);
        }
    }
}

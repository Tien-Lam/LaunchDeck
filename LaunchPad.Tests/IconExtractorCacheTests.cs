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
}

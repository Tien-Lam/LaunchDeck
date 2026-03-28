using LaunchPad.Shared;

namespace LaunchPad.Tests;

public class ConfigLoaderPathTests
{
    [Fact]
    public void GetDefaultConfigPath_EndsWithExpectedPath()
    {
        var path = ConfigLoader.GetDefaultConfigPath();

        Assert.EndsWith(@"LaunchPad\config.json", path);
    }

    [Fact]
    public void GetDefaultConfigPath_DoesNotContainPackages()
    {
        var path = ConfigLoader.GetDefaultConfigPath();

        Assert.DoesNotContain(@"\Packages\", path);
    }
}

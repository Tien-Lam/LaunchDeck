using LaunchDeck.Companion;
using Xunit;

namespace LaunchDeck.Tests;

public class FocusTargetResolverTests
{
    [Fact]
    public void GetCandidateProcessNames_ExePath_UsesExecutableName()
    {
        var candidates = FocusTargetResolver.GetCandidateProcessNames(null, "exe", @"C:\Tools\MyApp.exe");

        Assert.Contains("MyApp", candidates);
    }

    [Theory]
    [InlineData("\"C:\\Program Files\\Google\\Chrome\\Application\\chrome.exe\" --single-argument %1", @"C:\Program Files\Google\Chrome\Application\chrome.exe")]
    [InlineData("C:\\Program Files\\Mozilla Firefox\\firefox.exe -osint -url \"%1\"", @"C:\Program Files\Mozilla Firefox\firefox.exe")]
    [InlineData("\"C:\\Program Files (x86)\\Microsoft\\Edge\\Application\\msedge.exe\" --single-argument %1", @"C:\Program Files (x86)\Microsoft\Edge\Application\msedge.exe")]
    public void ExtractExecutablePathFromCommand_ReturnsExecutablePath(string command, string expected)
    {
        Assert.Equal(expected, FocusTargetResolver.ExtractExecutablePathFromCommand(command));
    }
}

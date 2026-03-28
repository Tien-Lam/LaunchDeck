using LaunchPad.Companion.Editor;

namespace LaunchPad.Tests;

public class ListBoxEntryTests
{
    [Theory]
    [InlineData("exe", "EXE")]
    [InlineData("url", "URL")]
    [InlineData("store", "APP")]
    [InlineData("anything", "APP")]
    public void TypeIcon_ReturnsCorrectLabel(string type, string expected)
    {
        var entry = new ListBoxEntry("Test", type);
        Assert.Equal(expected, entry.TypeIcon);
    }

    [Fact]
    public void ToString_IncludesNameAndType()
    {
        var entry = new ListBoxEntry("Notepad", "exe");
        Assert.Equal("Notepad  [exe]", entry.ToString());
    }
}

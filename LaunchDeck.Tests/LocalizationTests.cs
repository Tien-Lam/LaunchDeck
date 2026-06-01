using System.Globalization;
using LaunchDeck.Companion.Localization;
using Xunit;

namespace LaunchDeck.Tests;

public class LocalizationTests
{
    [Fact]
    public void Strings_Get_ReturnsNeutralString()
    {
        using var culture = new CultureScope("en-US");

        Assert.Equal("Save and Refresh", Strings.Get("SaveAndRefresh"));
    }

    [Fact]
    public void Strings_Get_ReturnsLocalizedString()
    {
        using var culture = new CultureScope("es-ES");

        Assert.Equal("Guardar y actualizar", Strings.Get("SaveAndRefresh"));
    }

    [Fact]
    public void Strings_Format_UsesLocalizedTemplate()
    {
        using var culture = new CultureScope("de-DE");

        Assert.Equal("3 Elemente", Strings.Format("ItemCountMany", 3));
    }

    private sealed class CultureScope : IDisposable
    {
        private readonly CultureInfo _originalCulture = CultureInfo.CurrentCulture;
        private readonly CultureInfo _originalUiCulture = CultureInfo.CurrentUICulture;

        public CultureScope(string name)
        {
            var culture = CultureInfo.GetCultureInfo(name);
            CultureInfo.CurrentCulture = culture;
            CultureInfo.CurrentUICulture = culture;
        }

        public void Dispose()
        {
            CultureInfo.CurrentCulture = _originalCulture;
            CultureInfo.CurrentUICulture = _originalUiCulture;
        }
    }
}

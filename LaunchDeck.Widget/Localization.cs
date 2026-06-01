using System;
using System.Globalization;
using Windows.ApplicationModel.Resources;

namespace LaunchDeck.Widget;

internal static class Localization
{
    private static ResourceLoader? _loader;

    internal static string Get(string key)
    {
        try
        {
            _loader ??= ResourceLoader.GetForCurrentView();
            var value = _loader.GetString(key);
            return string.IsNullOrEmpty(value) ? key : value;
        }
        catch
        {
            return key;
        }
    }

    internal static string Format(string key, params object[] args)
    {
        return string.Format(CultureInfo.CurrentUICulture, Get(key), args);
    }
}

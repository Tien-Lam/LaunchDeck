using System;
using System.Globalization;
using System.Resources;
using LaunchDeck.Shared;

namespace LaunchDeck.Companion.Localization;

internal static class Strings
{
    private static readonly ResourceManager Manager =
        new("LaunchDeck.Companion.Resources.Strings", typeof(Strings).Assembly);

    internal static string Get(string key)
    {
        return Manager.GetString(key, CultureInfo.CurrentUICulture) ?? key;
    }

    internal static string Format(string key, params object[] args)
    {
        return string.Format(CultureInfo.CurrentUICulture, Get(key), args);
    }

    internal static string TypeLabel(LaunchItemType type)
    {
        return type switch
        {
            LaunchItemType.Exe => Get("TypeExe"),
            LaunchItemType.Url => Get("TypeUrl"),
            LaunchItemType.Store => Get("TypeStore"),
            _ => type.ToString()
        };
    }
}

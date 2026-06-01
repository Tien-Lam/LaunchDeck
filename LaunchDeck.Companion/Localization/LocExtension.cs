using System;
using System.Windows.Markup;

namespace LaunchDeck.Companion.Localization;

public sealed class LocExtension : MarkupExtension
{
    public LocExtension(string key)
    {
        Key = key;
    }

    public string Key { get; }

    public override object ProvideValue(IServiceProvider serviceProvider)
    {
        return Strings.Get(Key);
    }
}

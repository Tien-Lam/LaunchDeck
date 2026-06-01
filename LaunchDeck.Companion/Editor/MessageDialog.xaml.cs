using System.Windows;
using System.Windows.Controls;
using LaunchDeck.Companion.Localization;

namespace LaunchDeck.Companion.Editor;

public partial class MessageDialog : Window
{
    public MessageBoxResult Result { get; private set; } = MessageBoxResult.None;

    private MessageDialog()
    {
        InitializeComponent();
    }

    public static MessageBoxResult Show(
        Window owner,
        string message,
        string title,
        MessageBoxButton buttons)
    {
        var dialog = new MessageDialog { Owner = owner };
        dialog.TitleText.Text = title;
        dialog.MessageText.Text = message;
        dialog.BuildButtons(buttons);
        dialog.ShowDialog();
        return dialog.Result;
    }

    private void BuildButtons(MessageBoxButton buttons)
    {
        switch (buttons)
        {
            case MessageBoxButton.YesNo:
                AddButton(Strings.Get("Yes"), MessageBoxResult.Yes, accent: true);
                AddButton(Strings.Get("No"), MessageBoxResult.No);
                break;
            case MessageBoxButton.YesNoCancel:
                AddButton(Strings.Get("Save"), MessageBoxResult.Yes, accent: true);
                AddButton(Strings.Get("DontSave"), MessageBoxResult.No);
                AddButton(Strings.Get("Cancel"), MessageBoxResult.Cancel);
                break;
            case MessageBoxButton.OKCancel:
                AddButton(Strings.Get("Ok"), MessageBoxResult.OK, accent: true);
                AddButton(Strings.Get("Cancel"), MessageBoxResult.Cancel);
                break;
            default:
                AddButton(Strings.Get("Ok"), MessageBoxResult.OK, accent: true);
                break;
        }
    }

    private void AddButton(string text, MessageBoxResult result, bool accent = false)
    {
        var button = new Button
        {
            Content = text,
            MinWidth = 80,
            Margin = new Thickness(ButtonPanel.Children.Count > 0 ? 8 : 0, 0, 0, 0)
        };

        if (accent)
            button.Style = (Style)FindResource("AccentButtonStyle");

        button.Click += (_, _) =>
        {
            Result = result;
            Close();
        };

        ButtonPanel.Children.Add(button);
    }
}

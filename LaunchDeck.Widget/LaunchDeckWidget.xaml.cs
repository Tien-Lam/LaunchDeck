using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.ApplicationModel;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Animation;
using Windows.UI.Xaml.Media.Imaging;
using LaunchDeck.Shared;
using LaunchDeck.Widget.Models;
using LaunchDeck.Widget.Services;
using Microsoft.Gaming.XboxGameBar;

namespace LaunchDeck.Widget;

public sealed partial class LaunchDeckWidget : Page
{
    private bool _eventsSubscribed;
    private int _loadGeneration;
    private int _launchGeneration;
    private bool _focusLaunchedApps;
    private const int FocusAfterCompanionLaunchDelayMs = 350;
    public ObservableCollection<LaunchItem> Items { get; } = new();

    public LaunchDeckWidget()
    {
        this.InitializeComponent();
        this.Loaded += OnLoaded;
    }

    public async void ReloadAsync()
    {
        // Re-establish companion connection and reload config
        // Called on widget re-activation (close then re-add from Game Bar)
        await EnsureCompanionAsync();
        await LoadConfigAsync(requestInitialFocus: true);
    }

    private async Task EnsureCompanionAsync()
    {
        for (int attempt = 0; attempt < 3; attempt++)
        {
            try
            {
                await FullTrustProcessLauncher.LaunchFullTrustProcessForCurrentAppAsync();
            }
            catch (Exception)
            {
                // Companion may already be running
            }

            for (int i = 0; i < 100 && App.CompanionConnection == null; i++)
                await Task.Delay(100);

            if (App.CompanionConnection != null) break;

            await Task.Delay(1000);
        }
    }

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        await EnsureCompanionAsync();
        await LoadConfigAsync(requestInitialFocus: true);

        if (!_eventsSubscribed)
        {
            _eventsSubscribed = true;
            CompanionClient.ConfigUpdated += async () =>
            {
                await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, async () =>
                {
                    await LoadConfigAsync();
                });
            };
            CompanionClient.CompanionConnected += async () =>
            {
                await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, async () =>
                {
                    await LoadConfigAsync();
                });
            };

            var widget = App.Widget;
            if (widget != null)
            {
                // Reload config when Game Bar overlay becomes visible
                // Catches saves that were missed while the widget was suspended
                widget.VisibleChanged += async (s, a) =>
                {
                    if (s.Visible)
                    {
                        await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, async () =>
                        {
                            await LoadConfigAsync();
                        });
                    }
                };

                // Honor Game Bar opacity setting for compact/pinned mode
                try
                {
                    ApplyBackgroundOpacity(widget.RequestedOpacity / 100.0);
                    widget.RequestedOpacityChanged += (opacitySender, args) =>
                    {
                        _ = Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
                        {
                            ApplyBackgroundOpacity(opacitySender.RequestedOpacity / 100.0);
                        });
                    };
                }
                catch
                {
                    // RequestedOpacity may not be available in all Game Bar versions
                }
            }
        }
    }

    private async Task LoadConfigAsync(bool requestInitialFocus = false)
    {
        var loadGeneration = ++_loadGeneration;
        (ConfigLoadStatus status, LaunchDeckConfig? config, string? configPath, string? error) result;
        try
        {
            result = await CompanionClient.LoadConfigAsync();
        }
        catch (Exception ex)
        {
            if (loadGeneration != _loadGeneration) return;
            ShowEmptyState("Load error", ex.Message);
            return;
        }
        if (loadGeneration != _loadGeneration) return;

        var (status, config, configPath, error) = result;

        if (status == ConfigLoadStatus.FileNotFound)
        {
            ShowEmptyState("No apps configured",
                "Click the gear button to add apps");
            return;
        }

        if (status == ConfigLoadStatus.ParseError)
        {
            ShowEmptyState("Invalid config file",
                $"JSON parse error:\n{error}");
            return;
        }

        if (config == null || config.Items.Count == 0)
        {
            ShowEmptyState("No apps configured",
                "Click the gear button to add apps");
            return;
        }

        _focusLaunchedApps = config.FocusLaunchedApps;

        Items.Clear();
        foreach (var item in config.Items)
        {
            var launchItem = new LaunchItem
            {
                Name = item.Name,
                Type = item.Type.ToString().ToLowerInvariant(),
                Path = item.Path,
                Args = item.Args,
                CustomIconPath = item.Icon
            };
            Items.Add(launchItem);
        }

        ItemsScrollViewer.Visibility = Visibility.Visible;
        EmptyState.Visibility = Visibility.Collapsed;
        LoadingState.Visibility = Visibility.Visible;

        if (Items.Count > 0)
            await SelectFirstTileAsync(loadGeneration, requestInitialFocus);

        var items = Items.ToArray();
        await LoadIconsAsync(items, loadGeneration);

        if (loadGeneration != _loadGeneration) return;
        LoadingState.Visibility = Visibility.Collapsed;
    }

    private async Task SelectFirstTileAsync(int loadGeneration, bool requestFocus)
    {
        // Do not let a queued focus request run after a launch has started.
        var launchGeneration = _launchGeneration;
        await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Low, () =>
        {
            if (loadGeneration != _loadGeneration || Items.Count == 0)
                return;

            ItemsGrid.SelectedIndex = 0;

            if (!requestFocus || launchGeneration != _launchGeneration || !CanRequestTileFocus())
                return;

            var firstContainer = ItemsGrid.ContainerFromIndex(0) as GridViewItem;
            firstContainer?.Focus(FocusState.Keyboard);
        });
    }

    private static bool CanRequestTileFocus()
    {
        var widget = App.Widget;
        if (widget == null)
            return false;

        try
        {
            return widget.Visible && !widget.Pinned;
        }
        catch
        {
            return false;
        }
    }

    private async Task LoadIconsAsync(LaunchItem[] items, int loadGeneration)
    {
        foreach (var item in items)
        {
            if (loadGeneration != _loadGeneration) return;

            byte[]? iconData = null;

            // Custom icon takes priority over type-based extraction
            if (!string.IsNullOrEmpty(item.CustomIconPath))
            {
                iconData = await CompanionClient.LoadCustomIconAsync(item.CustomIconPath!);
            }

            // Fall back to type-based extraction
            if (iconData == null)
            {
                if (item.Type == "exe")
                {
                    iconData = await CompanionClient.ExtractIconAsync(item.Path);
                }
                else if (item.Type == "url")
                {
                    iconData = await CompanionClient.FetchFaviconAsync(item.Path);
                }
                else if (item.Type == "store")
                {
                    var aumid = ExtractAumidFromPath(item.Path);
                    if (aumid != null)
                        iconData = await CompanionClient.ExtractStoreIconAsync(aumid);
                }
            }

            if (iconData != null)
            {
                try
                {
                    var bitmap = new BitmapImage();
                    using (var stream = new Windows.Storage.Streams.InMemoryRandomAccessStream())
                    {
                        await stream.WriteAsync(iconData.AsBuffer());
                        stream.Seek(0);
                        await bitmap.SetSourceAsync(stream);
                    }
                    if (loadGeneration != _loadGeneration) return;
                    item.IconSource = bitmap;
                }
                catch
                {
                    if (loadGeneration != _loadGeneration) return;
                    SetDefaultIcon(item);
                }
            }
            else
            {
                if (loadGeneration != _loadGeneration) return;
                SetDefaultIcon(item);
            }
        }
    }

    private void ApplyBackgroundOpacity(double opacity)
    {
        var alpha = (byte)(opacity * 255);
        this.Background = new SolidColorBrush(Windows.UI.Color.FromArgb(alpha, 0x20, 0x20, 0x20));
    }

    private static string? ExtractAumidFromPath(string path)
    {
        const string prefix = @"shell:AppsFolder\";
        if (path.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            return path.Substring(prefix.Length);
        return null;
    }

    private void SetDefaultIcon(LaunchItem item)
    {
        var assetName = item.Type == "url" ? "DefaultGlobe.png" : "DefaultApp.png";
        item.IconSource = new BitmapImage(new Uri($"ms-appx:///Assets/{assetName}"));
    }

    private void ShowEmptyState(string title, string message)
    {
        ItemsScrollViewer.Visibility = Visibility.Collapsed;
        LoadingState.Visibility = Visibility.Collapsed;
        EmptyState.Visibility = Visibility.Visible;
        EmptyStateTitle.Text = title;
        EmptyStateMessage.Text = message;
    }

    private async void OnItemClick(object sender, ItemClickEventArgs e)
    {
        if (e.ClickedItem is LaunchItem item)
        {
            _launchGeneration++;
            bool success;
            bool dismissAfterCompanionLaunch = false;
            var widget = App.Widget;
            var launchId = Guid.NewGuid().ToString("N");
            var focusDelayMs = _focusLaunchedApps ? FocusAfterCompanionLaunchDelayMs : 0;
            CompanionClient.RemoteLog($"widget-launch[{launchId}]: click type={item.Type} path={item.Path} argsPresent={!string.IsNullOrWhiteSpace(item.Args)} focusSetting={_focusLaunchedApps}");

            if (item.Type == "url" || item.Type == "store")
            {
                // Use Game Bar's built-in launcher so Game Bar owns overlay dismissal.
                try
                {
                    CompanionClient.RemoteLog($"widget-launch[{launchId}]: using LaunchUriAsync");
                    success = widget != null
                        ? await widget.LaunchUriAsync(new Uri(item.Path))
                        : await CompanionClient.LaunchAsync(item.Type, item.Path, item.Args, _focusLaunchedApps, launchId, focusDelayMs);
                    dismissAfterCompanionLaunch = widget == null && success;
                }
                catch
                {
                    CompanionClient.RemoteLog($"widget-launch[{launchId}]: LaunchUriAsync failed; falling back to companion");
                    success = await CompanionClient.LaunchAsync(item.Type, item.Path, item.Args, _focusLaunchedApps, launchId, focusDelayMs);
                    dismissAfterCompanionLaunch = success;
                }
            }
            else if (item.Type == "exe")
            {
                if (_focusLaunchedApps)
                {
                    success = await CompanionClient.LaunchAsync(
                        item.Type,
                        item.Path,
                        item.Args,
                        focusLaunchedApp: true,
                        launchId: launchId,
                        focusDelayMs: FocusAfterCompanionLaunchDelayMs);
                    dismissAfterCompanionLaunch = success;
                }
                // Try LaunchUriAsync with file: URI so Game Bar owns overlay dismissal.
                // Falls back to companion Process.Start if it fails (e.g. exe with args)
                else if (widget != null && string.IsNullOrEmpty(item.Args))
                {
                    try
                    {
                        CompanionClient.RemoteLog($"widget-launch[{launchId}]: using LaunchUriAsync");
                        var fileUri = new Uri(item.Path);
                        success = await widget.LaunchUriAsync(fileUri);
                    }
                    catch
                    {
                        CompanionClient.RemoteLog($"widget-launch[{launchId}]: LaunchUriAsync failed; falling back to companion");
                        success = await CompanionClient.LaunchAsync(item.Type, item.Path, item.Args, _focusLaunchedApps, launchId, focusDelayMs);
                        dismissAfterCompanionLaunch = success;
                    }
                }
                else
                {
                    success = await CompanionClient.LaunchAsync(item.Type, item.Path, item.Args, _focusLaunchedApps, launchId, focusDelayMs);
                    dismissAfterCompanionLaunch = success;
                }
            }
            else
            {
                success = await CompanionClient.LaunchAsync(item.Type, item.Path, item.Args, _focusLaunchedApps, launchId, focusDelayMs);
                dismissAfterCompanionLaunch = success;
            }

            if (sender is GridView gridView)
            {
                var container = gridView.ContainerFromItem(item) as GridViewItem;
                if (container != null)
                {
                    var overlay = FindChild<Border>(container, "FeedbackOverlay");
                    if (overlay != null)
                    {
                        overlay.Background = success
                            ? (SolidColorBrush)Resources["LaunchSuccessBrush"]
                            : (SolidColorBrush)Resources["LaunchFailureBrush"];

                        var fadeOut = new DoubleAnimation
                        {
                            From = 1.0,
                            To = 0.0,
                            Duration = new Duration(TimeSpan.FromMilliseconds(400)),
                            EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseIn }
                        };
                        Storyboard.SetTarget(fadeOut, overlay);
                        Storyboard.SetTargetProperty(fadeOut, "Opacity");
                        var sb = new Storyboard();
                        sb.Children.Add(fadeOut);
                        sb.Begin();
                    }
                }
            }

            if (dismissAfterCompanionLaunch)
                await TryMinimizeAfterCompanionLaunchAsync(launchId);
        }
    }

    private static async Task TryMinimizeAfterCompanionLaunchAsync(string launchId)
    {
        var widget = App.Widget;
        if (widget == null)
            return;

        try
        {
            if (!widget.Pinned)
            {
                CompanionClient.RemoteLog($"widget-launch[{launchId}]: minimizing unpinned widget after companion launch");
                await widget.MinimizeAsync();
            }
            else
            {
                CompanionClient.RemoteLog($"widget-launch[{launchId}]: widget pinned; leaving visible after companion launch");
            }
        }
        catch (Exception ex)
        {
            // Some Game Bar host states reject MinimizeAsync; launching still succeeded.
            CompanionClient.RemoteLog($"widget-launch[{launchId}]: MinimizeAsync failed {ex.GetType().Name}: {ex.Message}");
        }
    }

    private Windows.UI.Color GetResourceColor(string key) =>
        ((SolidColorBrush)Resources[key]).Color;

    private void OnTilePointerEntered(object sender, PointerRoutedEventArgs e)
    {
        if (sender is Grid grid)
            AnimateTileBackground(grid, GetResourceColor("TileBackgroundHover"), TimeSpan.FromMilliseconds(150));
    }

    private void OnTilePointerExited(object sender, PointerRoutedEventArgs e)
    {
        if (sender is Grid grid)
        {
            AnimateTileBackground(grid, GetResourceColor("TileBackground"), TimeSpan.FromMilliseconds(150));
            AnimateTileScale(grid, 1.0, TimeSpan.FromMilliseconds(100));
        }
    }

    private void OnTilePointerPressed(object sender, PointerRoutedEventArgs e)
    {
        if (sender is Grid grid)
        {
            AnimateTileBackground(grid, GetResourceColor("TileBackgroundPressed"), TimeSpan.FromMilliseconds(100));
            AnimateTileScale(grid, 0.95, TimeSpan.FromMilliseconds(100));
        }
    }

    private void OnTilePointerReleased(object sender, PointerRoutedEventArgs e)
    {
        if (sender is Grid grid)
        {
            AnimateTileBackground(grid, GetResourceColor("TileBackgroundHover"), TimeSpan.FromMilliseconds(100));
            AnimateTileScale(grid, 1.0, TimeSpan.FromMilliseconds(100));
        }
    }

    private void OnTileGotFocus(object sender, RoutedEventArgs e)
    {
        if (sender is Grid grid)
        {
            var border = FindChild<Border>(grid, "FocusBorder");
            if (border != null) border.Opacity = 1;
            AnimateTileBackground(grid, GetResourceColor("TileBackgroundHover"), TimeSpan.FromMilliseconds(100));
        }
    }

    private void OnTileLostFocus(object sender, RoutedEventArgs e)
    {
        if (sender is Grid grid)
        {
            var border = FindChild<Border>(grid, "FocusBorder");
            if (border != null) border.Opacity = 0;
            AnimateTileBackground(grid, GetResourceColor("TileBackground"), TimeSpan.FromMilliseconds(100));
        }
    }

    private async void OnEditClick(object sender, RoutedEventArgs e)
    {
        EditButton.IsEnabled = false;
        try
        {
            await CompanionClient.OpenEditorAsync();
        }
        finally
        {
            EditButton.IsEnabled = true;
        }
    }

    private static void AnimateTileBackground(Grid grid, Windows.UI.Color color, TimeSpan duration)
    {
        var animation = new ColorAnimation
        {
            To = color,
            Duration = new Duration(duration),
            EnableDependentAnimation = true
        };
        Storyboard.SetTarget(animation, grid.Background);
        Storyboard.SetTargetProperty(animation, "Color");
        var sb = new Storyboard();
        sb.Children.Add(animation);
        sb.Begin();
    }

    private static void AnimateTileScale(Grid grid, double scale, TimeSpan duration)
    {
        if (grid.RenderTransform is CompositeTransform transform)
        {
            var scaleX = new DoubleAnimation { To = scale, Duration = new Duration(duration) };
            var scaleY = new DoubleAnimation { To = scale, Duration = new Duration(duration) };
            Storyboard.SetTarget(scaleX, transform);
            Storyboard.SetTargetProperty(scaleX, "ScaleX");
            Storyboard.SetTarget(scaleY, transform);
            Storyboard.SetTargetProperty(scaleY, "ScaleY");
            var sb = new Storyboard();
            sb.Children.Add(scaleX);
            sb.Children.Add(scaleY);
            sb.Begin();
        }
    }

    private static T? FindChild<T>(DependencyObject parent, string? name = null) where T : DependencyObject
    {
        var count = VisualTreeHelper.GetChildrenCount(parent);
        for (int i = 0; i < count; i++)
        {
            var child = VisualTreeHelper.GetChild(parent, i);
            if (child is T found)
            {
                if (name == null || (found is FrameworkElement fe && fe.Name == name))
                    return found;
            }
            var result = FindChild<T>(child, name);
            if (result != null) return result;
        }
        return null;
    }
}

using System;
using System.Threading.Tasks;
using Windows.ApplicationModel;
using Windows.ApplicationModel.Activation;
using Windows.ApplicationModel.AppService;
using Windows.ApplicationModel.Background;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Microsoft.Gaming.XboxGameBar;

namespace LaunchDeck.Widget;

sealed partial class App : Application
{
    private XboxGameBarWidget? _widget;
    private AppServiceConnection? _companionConnection;
    private BackgroundTaskDeferral? _appServiceDeferral;
    private Task? _companionRelaunchTask;

    public static AppServiceConnection? CompanionConnection { get; private set; }
    public static XboxGameBarWidget? Widget { get; private set; }

    public App()
    {
        this.InitializeComponent();
        this.Suspending += OnSuspending;
    }

    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
        // Widget only works via Game Bar protocol activation. Close if launched directly.
        Current.Exit();
    }

    protected override void OnActivated(IActivatedEventArgs args)
    {
        if (args.Kind == ActivationKind.Protocol)
        {
            var protocolArgs = args as IProtocolActivatedEventArgs;

            if (protocolArgs?.Uri.Scheme == "ms-gamebarwidget")
            {
                var widgetArgs = args as XboxGameBarWidgetActivatedEventArgs;
                if (widgetArgs != null)
                {
                    if (!widgetArgs.IsLaunchActivation)
                    {
                        ReloadActiveWidget();
                        Window.Current.Activate();
                        return;
                    }

                    ReleaseWidget();

                    var rootFrame = new Frame();
                    Window.Current.Content = rootFrame;

                    _widget = new XboxGameBarWidget(
                        widgetArgs,
                        Window.Current.CoreWindow,
                        rootFrame);
                    Widget = _widget;

                    rootFrame.Navigate(typeof(LaunchDeckWidget));
                    Window.Current.Closed += OnWindowClosed;
                    Window.Current.Activate();
                }
            }
        }
    }

    private static void ReloadActiveWidget()
    {
        if (Window.Current.Content is Frame { Content: LaunchDeckWidget page })
            page.ReloadAsync();
    }

    private void OnWindowClosed(object sender, Windows.UI.Core.CoreWindowEventArgs e)
    {
        ReleaseWidget();
    }

    private void ReleaseWidget()
    {
        try
        {
            Window.Current.Closed -= OnWindowClosed;
        }
        catch
        {
        }

        if (Window.Current.Content is Frame { Content: LaunchDeckWidget page })
            page.ReleaseForWidgetLifecycle();

        _widget = null;
        Widget = null;
    }


    protected override void OnBackgroundActivated(BackgroundActivatedEventArgs args)
    {
        base.OnBackgroundActivated(args);

        if (args.TaskInstance.TriggerDetails is AppServiceTriggerDetails details)
        {
            _appServiceDeferral = args.TaskInstance.GetDeferral();
            _companionConnection = details.AppServiceConnection;
            CompanionConnection = _companionConnection;

            Services.CompanionClient.RaiseCompanionConnected();

            _companionConnection.RequestReceived += Services.CompanionClient.OnCompanionMessage;
            _companionConnection.ServiceClosed += (_, _) =>
            {
                CompanionConnection = null;
                ScheduleCompanionRelaunch();
            };

            args.TaskInstance.Canceled += (_, _) =>
            {
                // Dispose connection to trigger ServiceClosed on the companion side,
                // so it releases the mutex and exits instead of becoming a zombie
                try { _companionConnection?.Dispose(); }
                catch { }
                _companionConnection = null;
                CompanionConnection = null;
                _appServiceDeferral?.Complete();
            };
        }
    }

    private void ScheduleCompanionRelaunch()
    {
        if (_companionRelaunchTask is { IsCompleted: false })
            return;

        _companionRelaunchTask = TryRelaunchCompanionAsync();
    }

    private static async Task TryRelaunchCompanionAsync()
    {
        int[] delays = { 1000, 2000, 4000 };
        foreach (var delay in delays)
        {
            await Task.Delay(delay);
            if (CompanionConnection != null) return;
            try
            {
                await FullTrustProcessLauncher.LaunchFullTrustProcessForCurrentAppAsync();
            }
            catch { }
        }
    }

    private void OnSuspending(object sender, SuspendingEventArgs e)
    {
        var deferral = e.SuspendingOperation.GetDeferral();
        ReleaseWidget();
        CompanionConnection = null;
        deferral.Complete();
    }
}

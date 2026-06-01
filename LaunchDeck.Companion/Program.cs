using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using LaunchDeck.Shared;
using Windows.ApplicationModel;
using Windows.ApplicationModel.AppService;
using Windows.Foundation.Collections;

namespace LaunchDeck.Companion;

class Program
{
    private const string MutexName = "Local\\LaunchDeckCompanion";
    private const string ReconnectRequestEventNamePrefix = "Local\\LaunchDeckCompanionReconnectRequest_";
    private const string ReconnectAcknowledgedEventNamePrefix = "Local\\LaunchDeckCompanionReconnectAcknowledged_";

    private static AppServiceConnection? _connection;
    private static readonly ManualResetEvent ExitEvent = new(false);
    private static readonly AutoResetEvent ReconnectEvent = new(false);
    private static EventWaitHandle? _reconnectAcknowledgedEvent;
    private static int _reconnectRequested;

    static async Task<int> Main(string[] args)
    {
        if (args.Length > 0 && string.Equals(args[0], "--diagnose-launch", StringComparison.OrdinalIgnoreCase))
            return await LaunchDiagnostics.RunLaunchDiagnosticAsync(args);

        Log.Write($"Companion starting. PFN={Package.Current.Id.FamilyName} ConfigPath={ConfigLoader.GetDefaultConfigPath()}");

        var packageScope = GetPackageScope();
        using var reconnectRequestEvent = new EventWaitHandle(false, EventResetMode.AutoReset, ReconnectRequestEventNamePrefix + packageScope);
        using var reconnectAcknowledgedEvent = new EventWaitHandle(false, EventResetMode.AutoReset, ReconnectAcknowledgedEventNamePrefix + packageScope);
        _reconnectAcknowledgedEvent = reconnectAcknowledgedEvent;

        // Only one current companion should own the App Service connection.
        // Same-version duplicate launches can ask the owner to reopen the App
        // Service. Different-version owners will not see the version-scoped
        // request, so updates replace stale binaries instead of reconnecting to them.
        using var mutex = new Mutex(false, MutexName);
        var ownsMutex = TryAcquireMutex(mutex, TimeSpan.FromSeconds(2));
        if (!ownsMutex)
        {
            if (RequestExistingCompanionReconnect(reconnectRequestEvent, reconnectAcknowledgedEvent))
                return 0;

            Log.Write("Existing companion did not acknowledge reconnect; terminating stale companion instances");
            TerminateOtherCompanionProcesses();
            ownsMutex = TryAcquireMutex(mutex, TimeSpan.FromSeconds(5));
        }

        if (!ownsMutex)
        {
            Log.Write("Unable to acquire companion mutex after recovery attempt; exiting");
            return 1;
        }

        RegisteredWaitHandle? reconnectRegistration = null;
        try
        {
            reconnectRegistration = ThreadPool.RegisterWaitForSingleObject(
                reconnectRequestEvent,
                OnReconnectRequested,
                null,
                Timeout.InfiniteTimeSpan,
                executeOnlyOnce: false);

            return await RunAppServiceLoopAsync();
        }
        finally
        {
            reconnectRegistration?.Unregister(null);
            _reconnectAcknowledgedEvent = null;
            _connection = null;

            try
            {
                if (ownsMutex)
                    mutex.ReleaseMutex();
            }
            catch
            {
            }
        }
    }

    private static string GetPackageScope()
    {
        var id = Package.Current.Id;
        var version = id.Version;
        var versionText = $"{version.Major}.{version.Minor}.{version.Build}.{version.Revision}";
        return $"{SanitizeKernelObjectName(id.FamilyName)}_{versionText}";
    }

    private static string SanitizeKernelObjectName(string value)
    {
        foreach (var invalidChar in new[] { '\\', '/' })
            value = value.Replace(invalidChar, '_');

        return value;
    }

    private static bool TryAcquireMutex(Mutex mutex, TimeSpan timeout)
    {
        try
        {
            return mutex.WaitOne(timeout);
        }
        catch (AbandonedMutexException)
        {
            return true;
        }
    }

    private static bool RequestExistingCompanionReconnect(EventWaitHandle requestEvent, EventWaitHandle acknowledgedEvent)
    {
        try { acknowledgedEvent.Reset(); }
        catch { }

        Log.Write("Another companion instance is already running; requesting AppService reconnect");
        try
        {
            requestEvent.Set();
            if (acknowledgedEvent.WaitOne(TimeSpan.FromSeconds(3)))
            {
                Log.Write("Existing companion acknowledged reconnect; exiting duplicate launch");
                return true;
            }
        }
        catch (Exception ex)
        {
            Log.Write($"Reconnect request failed {ex.GetType().Name}: {ex.Message}");
        }

        return false;
    }

    private static async Task<int> RunAppServiceLoopAsync()
    {
        while (true)
        {
            ExitEvent.Reset();
            ReconnectEvent.Reset();
            Interlocked.Exchange(ref _reconnectRequested, 0);

            using var connection = new AppServiceConnection
            {
                AppServiceName = "com.launchdeck.service",
                PackageFamilyName = Package.Current.Id.FamilyName
            };

            _connection = connection;
            connection.RequestReceived += OnRequestReceived;
            connection.ServiceClosed += OnServiceClosed;

            var status = await connection.OpenAsync();
            Log.Write($"AppService.OpenAsync -> {status}");
            if (status != AppServiceConnectionStatus.Success)
            {
                _connection = null;
                return 1;
            }

            var signaled = WaitHandle.WaitAny(new WaitHandle[] { ExitEvent, ReconnectEvent });

            connection.RequestReceived -= OnRequestReceived;
            connection.ServiceClosed -= OnServiceClosed;
            _connection = null;

            if (signaled == 1)
            {
                Log.Write("Reopening AppService connection after reconnect request");
                continue;
            }

            return 0;
        }
    }

    private static void OnServiceClosed(AppServiceConnection sender, AppServiceClosedEventArgs args)
    {
        if (Interlocked.CompareExchange(ref _reconnectRequested, 0, 0) == 1)
        {
            Log.Write("ServiceClosed during reconnect");
            ReconnectEvent.Set();
            return;
        }

        Log.Write("ServiceClosed - exiting");
        ExitEvent.Set();
    }

    private static void OnReconnectRequested(object? state, bool timedOut)
    {
        if (timedOut)
            return;

        Log.Write("AppService reconnect requested by duplicate companion launch");
        Interlocked.Exchange(ref _reconnectRequested, 1);

        try { _reconnectAcknowledgedEvent?.Set(); }
        catch { }

        ReconnectEvent.Set();
    }

    private static void TerminateOtherCompanionProcesses()
    {
        var currentProcessId = Environment.ProcessId;
        foreach (var process in Process.GetProcessesByName("LaunchDeck.Companion"))
        {
            using (process)
            {
                if (process.Id == currentProcessId)
                    continue;

                try
                {
                    Log.Write($"Terminating stale companion pid={process.Id}");
                    process.Kill(entireProcessTree: false);
                    process.WaitForExit(2000);
                }
                catch (Exception ex)
                {
                    Log.Write($"Failed to terminate companion pid={process.Id}: {ex.GetType().Name}: {ex.Message}");
                }
            }
        }
    }

    public static async void NotifyConfigUpdated()
    {
        var connection = _connection;
        if (connection == null) return;
        try
        {
            var message = new ValueSet { ["action"] = "config-updated" };
            await connection.SendMessageAsync(message);
        }
        catch { }
    }

    private static async void OnRequestReceived(AppServiceConnection sender, AppServiceRequestReceivedEventArgs args)
    {
        var deferral = args.GetDeferral();
        try
        {
            var message = args.Request.Message;
            var action = message["action"] as string;

            ValueSet response;
            switch (action)
            {
                case "launch":
                    response = HandleLaunch(message);
                    break;
                case "extract-icon":
                    response = HandleExtractIcon(message);
                    break;
                case "fetch-favicon":
                    response = await HandleFetchFaviconAsync(message);
                    break;
                case "load-config":
                    response = HandleLoadConfig(message);
                    break;
                case "open-editor":
                    response = HandleOpenEditor(message);
                    break;
                case "load-custom-icon":
                    response = HandleLoadCustomIcon(message);
                    break;
                case "extract-store-icon":
                    response = HandleExtractStoreIcon(message);
                    break;
                case "log":
                    Log.Write(message.ContainsKey("message") ? message["message"] as string ?? "" : "");
                    response = new ValueSet { ["status"] = "ok" };
                    break;
                default:
                    response = new ValueSet { ["status"] = "error", ["error"] = $"Unknown action: {action}" };
                    break;
            }

            await args.Request.SendResponseAsync(response);
        }
        catch (Exception ex)
        {
            var errorResponse = new ValueSet { ["status"] = "error", ["error"] = ex.Message };
            await args.Request.SendResponseAsync(errorResponse);
        }
        finally
        {
            deferral.Complete();
        }
    }

    private static ValueSet HandleLaunch(ValueSet message)
    {
        var type = message["type"] as string ?? "";
        var path = message["path"] as string ?? "";
        var args = message.ContainsKey("args") ? message["args"] as string : null;
        var focusLaunchedApp = IsTruthy(message, "focusLaunchedApp");
        var launchId = GetString(message, "launchId") ?? Guid.NewGuid().ToString("N");
        var focusDelayMs = GetInt(message, "focusDelayMs", focusLaunchedApp ? 300 : 0);

        Log.Write($"launch[{launchId}]: request type={type} path={path} argsPresent={!string.IsNullOrWhiteSpace(args)} focus={focusLaunchedApp} focusDelayMs={focusDelayMs}");

        var (success, error, process) = LaunchHandler.Launch(type, path, args);

        Log.Write($"launch[{launchId}]: result success={success} error={error ?? ""} pid={TryGetProcessId(process)?.ToString() ?? ""} process={TryGetProcessName(process) ?? ""}");

        if (success && process != null && focusLaunchedApp)
        {
            _ = Task.Run(async () =>
            {
                try
                {
                    if (focusDelayMs > 0)
                        await Task.Delay(focusDelayMs);

                    var focus = await NativeMethods.FocusProcessAsync(process, path);
                    Log.Write($"launch[{launchId}]: focus complete success={focus.Success} reason={focus.Reason} attempts={focus.Attempts} elapsedMs={focus.ElapsedMs} foreground={focus.ForegroundProcessName ?? ""}/{focus.ForegroundProcessId?.ToString() ?? ""} window={focus.ForegroundWindow ?? ""}");
                }
                catch (Exception ex)
                {
                    Log.Write($"launch[{launchId}]: focus failed with exception {ex.GetType().Name}: {ex.Message}");
                }
            });
        }

        var response = new ValueSet
        {
            ["status"] = success ? "ok" : "error",
            ["launchId"] = launchId
        };
        if (error != null) response["error"] = error;
        return response;
    }

    private static string? GetString(ValueSet message, string key)
    {
        return message.ContainsKey(key) ? message[key] as string : null;
    }

    private static int GetInt(ValueSet message, string key, int defaultValue)
    {
        if (!message.ContainsKey(key))
            return defaultValue;

        return message[key] switch
        {
            int value => value,
            string value when int.TryParse(value, out var parsed) => parsed,
            _ => defaultValue
        };
    }

    private static int? TryGetProcessId(System.Diagnostics.Process? process)
    {
        if (process == null)
            return null;

        try { return process.Id; }
        catch { return null; }
    }

    private static string? TryGetProcessName(System.Diagnostics.Process? process)
    {
        if (process == null)
            return null;

        try { return process.ProcessName; }
        catch { return null; }
    }

    private static bool IsTruthy(ValueSet message, string key)
    {
        if (!message.ContainsKey(key))
            return false;

        return message[key] switch
        {
            bool value => value,
            string value => bool.TryParse(value, out var parsed) && parsed,
            _ => false
        };
    }

    private static ValueSet HandleExtractIcon(ValueSet message)
    {
        var path = message["path"] as string ?? "";
        var cacheDir = IconExtractor.GetIconCacheDir();
        var (success, iconPath) = IconExtractor.ExtractFromExe(path, cacheDir);

        var response = new ValueSet { ["status"] = success ? "ok" : "error" };
        if (success && iconPath != null && System.IO.File.Exists(iconPath))
            response["iconData"] = Convert.ToBase64String(System.IO.File.ReadAllBytes(iconPath));
        return response;
    }

    private static async Task<ValueSet> HandleFetchFaviconAsync(ValueSet message)
    {
        var url = message["url"] as string ?? "";
        var cacheDir = IconExtractor.GetIconCacheDir();
        var (success, iconPath) = await IconExtractor.FetchFaviconAsync(url, cacheDir);

        var response = new ValueSet { ["status"] = success ? "ok" : "error" };
        if (success && iconPath != null && System.IO.File.Exists(iconPath))
            response["iconData"] = Convert.ToBase64String(System.IO.File.ReadAllBytes(iconPath));
        return response;
    }

    private static ValueSet HandleLoadConfig(ValueSet message)
    {
        var requestedPath = message.ContainsKey("configPath") ? message["configPath"] as string : null;
        var configPath = requestedPath ?? ConfigLoader.GetDefaultConfigPath();

        var result = ConfigLoader.Load(configPath);
        Log.Write($"load-config: path={configPath} status={result.Status} items={result.Config?.Items.Count ?? 0}");

        var response = new ValueSet
        {
            ["status"] = result.Status.ToString().ToLowerInvariant(),
            ["configPath"] = configPath
        };

        if (result.Status == ConfigLoadStatus.Success && result.Config != null)
        {
            var json = System.Text.Json.JsonSerializer.Serialize(result.Config,
                new System.Text.Json.JsonSerializerOptions { WriteIndented = false });
            response["json"] = json;
        }

        if (result.ErrorMessage != null)
            response["error"] = result.ErrorMessage;

        return response;
    }

    private static ValueSet HandleLoadCustomIcon(ValueSet message)
    {
        var path = message["path"] as string ?? "";
        var (success, data) = IconExtractor.LoadCustomIcon(path);

        var response = new ValueSet { ["status"] = success ? "ok" : "error" };
        if (success && data != null)
            response["iconData"] = Convert.ToBase64String(data);
        return response;
    }

    private static ValueSet HandleExtractStoreIcon(ValueSet message)
    {
        var aumid = message["aumid"] as string ?? "";
        var (success, data) = IconExtractor.ExtractStoreAppIcon(aumid);

        var response = new ValueSet { ["status"] = success ? "ok" : "error" };
        if (success && data != null)
            response["iconData"] = Convert.ToBase64String(data);
        return response;
    }

    private static ValueSet HandleOpenEditor(ValueSet message)
    {
        var configPath = message.ContainsKey("configPath")
            ? message["configPath"] as string ?? ConfigLoader.GetDefaultConfigPath()
            : ConfigLoader.GetDefaultConfigPath();

        Log.Write($"open-editor: configPath={configPath}");
        EditorManager.OpenEditor(configPath, NotifyConfigUpdated);
        return new ValueSet { ["status"] = "ok" };
    }
}

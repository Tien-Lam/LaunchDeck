using System;
using System.Text.Json;
using System.Threading.Tasks;
using LaunchPad.Shared;
using Windows.ApplicationModel.AppService;
using Windows.Foundation.Collections;

namespace LaunchPad.Widget.Services;

public static class CompanionClient
{
    public static async Task<(ConfigLoadStatus Status, LaunchPadConfig? Config, string? ConfigPath, string? Error)> LoadConfigAsync()
    {
        var connection = App.CompanionConnection;
        if (connection == null)
            return (ConfigLoadStatus.FileNotFound, null, null, "Companion not connected");

        var request = new ValueSet { ["action"] = "load-config" };
        var response = await connection.SendMessageAsync(request);
        if (response.Status != AppServiceResponseStatus.Success)
            return (ConfigLoadStatus.FileNotFound, null, null, "App Service error");

        var msg = response.Message;
        var status = msg["status"] as string;
        var configPath = msg.ContainsKey("configPath") ? msg["configPath"] as string : null;

        if (status == "success" && msg.ContainsKey("json"))
        {
            var json = msg["json"] as string;
            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var config = JsonSerializer.Deserialize<LaunchPadConfig>(json, options);
            return (ConfigLoadStatus.Success, config, configPath, null);
        }

        if (status == "filenotfound")
            return (ConfigLoadStatus.FileNotFound, null, configPath, null);

        var error = msg.ContainsKey("error") ? msg["error"] as string : null;
        return (ConfigLoadStatus.ParseError, null, configPath, error);
    }

    public static async Task<bool> LaunchAsync(string type, string path, string? args = null)
    {
        var connection = App.CompanionConnection;
        if (connection == null) return false;

        var request = new ValueSet
        {
            ["action"] = "launch",
            ["type"] = type,
            ["path"] = path
        };
        if (args != null) request["args"] = args;

        var response = await connection.SendMessageAsync(request);
        if (response.Status != AppServiceResponseStatus.Success) return false;

        return response.Message["status"] as string == "ok";
    }

    public static async Task<string?> ExtractIconAsync(string exePath)
    {
        var connection = App.CompanionConnection;
        if (connection == null) return null;

        var request = new ValueSet
        {
            ["action"] = "extract-icon",
            ["path"] = exePath
        };

        var response = await connection.SendMessageAsync(request);
        if (response.Status != AppServiceResponseStatus.Success) return null;

        if (response.Message["status"] as string == "ok")
            return response.Message["iconPath"] as string;

        return null;
    }

    public static async Task<string?> FetchFaviconAsync(string url)
    {
        var connection = App.CompanionConnection;
        if (connection == null) return null;

        var request = new ValueSet
        {
            ["action"] = "fetch-favicon",
            ["url"] = url
        };

        var response = await connection.SendMessageAsync(request);
        if (response.Status != AppServiceResponseStatus.Success) return null;

        if (response.Message["status"] as string == "ok")
            return response.Message["iconPath"] as string;

        return null;
    }

    public static async Task<(bool Success, string? Name, string? Path)> AddExeAsync(string configPath)
    {
        var connection = App.CompanionConnection;
        if (connection == null) return (false, null, null);

        var request = new ValueSet
        {
            ["action"] = "add-exe",
            ["configPath"] = configPath
        };

        var response = await connection.SendMessageAsync(request);
        if (response.Status != AppServiceResponseStatus.Success) return (false, null, null);

        var status = response.Message["status"] as string;
        if (status == "ok")
        {
            var name = response.Message["name"] as string;
            var path = response.Message["path"] as string;
            return (true, name, path);
        }

        return (false, null, null);
    }
}

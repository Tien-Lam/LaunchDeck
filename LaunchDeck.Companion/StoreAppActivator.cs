using System;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace LaunchDeck.Companion;

internal static class StoreAppActivator
{
    internal static (bool Success, string? Error, Process? Process) Activate(string aumid)
    {
        try
        {
            var activationManagerType = Type.GetTypeFromCLSID(ApplicationActivationManagerClsid, throwOnError: true)!;
            var manager = (IApplicationActivationManager)Activator.CreateInstance(activationManagerType)!;
            var hr = manager.ActivateApplication(aumid, null, ActivateOptions.None, out var processId);
            if (hr < 0)
                Marshal.ThrowExceptionForHR(hr);

            Process? process = null;
            try { process = Process.GetProcessById((int)processId); }
            catch { }

            return (true, null, process);
        }
        catch (Exception ex)
        {
            return (false, ex.Message, null);
        }
    }

    private static readonly Guid ApplicationActivationManagerClsid =
        new("45BA127D-10A8-46EA-8AB7-56EA9078943C");

    [Flags]
    private enum ActivateOptions
    {
        None = 0
    }

    [ComImport]
    [Guid("2e941141-7f97-4756-ba1d-9decde894a3d")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    private interface IApplicationActivationManager
    {
        int ActivateApplication(
            [MarshalAs(UnmanagedType.LPWStr)] string appUserModelId,
            [MarshalAs(UnmanagedType.LPWStr)] string? arguments,
            ActivateOptions options,
            out uint processId);

        int ActivateForFile(
            [MarshalAs(UnmanagedType.LPWStr)] string appUserModelId,
            nint itemArray,
            [MarshalAs(UnmanagedType.LPWStr)] string verb,
            out uint processId);

        int ActivateForProtocol(
            [MarshalAs(UnmanagedType.LPWStr)] string appUserModelId,
            nint itemArray,
            out uint processId);
    }
}

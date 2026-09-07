using System.IO;
using Microsoft.Win32;

namespace ResourcePins;

public record ResourceUsage(string Capability, IReadOnlyList<string> Apps);

/// <summary>
/// Reads the Windows ConsentStore (CapabilityAccessManager) to find which apps
/// are using a resource right now. An entry counts as in use when it has
/// LastUsedTimeStart > 0 and LastUsedTimeStop == 0.
/// </summary>
public static class UsageMonitor
{
    private const string ConsentStorePath =
        @"SOFTWARE\Microsoft\Windows\CurrentVersion\CapabilityAccessManager\ConsentStore";

    /// <summary>Capability keys as named by Windows in the registry.</summary>
    public static readonly string[] Capabilities =
    [
        "webcam",
        "microphone",
        "location",
        "graphicsCaptureProgrammatic",
        "graphicsCaptureWithoutBorder",
    ];

    public static List<ResourceUsage> GetActiveUsages()
    {
        var running = GetRunningProcessNames();
        var result = new List<ResourceUsage>();
        foreach (var capability in Capabilities)
        {
            var apps = new List<string>();
            // HKCU covers user apps, HKLM covers services and system components
            CollectApps(Registry.CurrentUser, capability, apps, running);
            CollectApps(Registry.LocalMachine, capability, apps, running);
            if (apps.Count > 0)
                result.Add(new ResourceUsage(capability, apps.Distinct().ToList()));
        }
        return result;
    }

    /// <summary>
    /// Names of the running processes. A process can exit between the enumeration
    /// and the name lookup, which throws, so each read is guarded on its own.
    /// </summary>
    private static HashSet<string> GetRunningProcessNames()
    {
        var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        System.Diagnostics.Process[] procs;
        try { procs = System.Diagnostics.Process.GetProcesses(); }
        catch { return names; }

        foreach (var p in procs)
        {
            try { names.Add(p.ProcessName); }
            catch { /* process exited, or is protected */ }
            finally { p.Dispose(); }
        }
        return names;
    }

    private static void CollectApps(RegistryKey hive, string capability, List<string> apps,
        HashSet<string> running)
    {
        using var capKey = hive.OpenSubKey($@"{ConsentStorePath}\{capability}");
        if (capKey is null) return;

        foreach (var subName in capKey.GetSubKeyNames())
        {
            using var subKey = capKey.OpenSubKey(subName);
            if (subKey is null) continue;

            if (subName.Equals("NonPackaged", StringComparison.OrdinalIgnoreCase))
            {
                // Classic desktop apps: one subkey per executable, path encoded with '#'
                foreach (var appName in subKey.GetSubKeyNames())
                {
                    using var appKey = subKey.OpenSubKey(appName);
                    if (!IsInUse(appKey)) continue;

                    // Windows does not always write LastUsedTimeStop when an app exits
                    // abnormally or updates mid-use, which leaves the key marked as in
                    // use forever. Require the executable to still exist and a matching
                    // process to be alive.
                    var exePath = appName.Replace('#', '\\');
                    var procName = Path.GetFileNameWithoutExtension(exePath);
                    if (File.Exists(exePath) && running.Contains(procName))
                        apps.Add(FriendlyNameFromNonPackaged(appName));
                }
            }
            else if (IsInUse(subKey))
            {
                // Packaged (Store/UWP) apps: the key name is the package family name
                apps.Add(FriendlyNameFromPackaged(subName));
            }
        }
    }

    private static bool IsInUse(RegistryKey? appKey)
    {
        if (appKey is null) return false;
        var stop = appKey.GetValue("LastUsedTimeStop");
        var start = appKey.GetValue("LastUsedTimeStart");
        return start is long s && s > 0 && stop is long e && e == 0;
    }

    private static string FriendlyNameFromNonPackaged(string keyName)
    {
        // "C:#Program Files#App#app.exe" -> "app.exe"
        var path = keyName.Replace('#', '\\');
        var exe = Path.GetFileName(path);
        return string.IsNullOrEmpty(exe) ? keyName : exe;
    }

    private static string FriendlyNameFromPackaged(string packageFamily)
    {
        // "Microsoft.WindowsCamera_8wekyb3d8bbwe" -> "Microsoft.WindowsCamera"
        var idx = packageFamily.LastIndexOf('_');
        return idx > 0 ? packageFamily[..idx] : packageFamily;
    }
}

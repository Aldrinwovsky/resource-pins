using System.IO;
using Microsoft.Win32;

namespace ResourcePins;

public record ResourceUsage(string Capability, IReadOnlyList<string> Apps);

/// <summary>
/// Lê o ConsentStore do Windows (CapabilityAccessManager) e diz quais apps
/// estão usando cada recurso AGORA (LastUsedTimeStop == 0 => em uso).
/// </summary>
public static class UsageMonitor
{
    private const string ConsentStorePath =
        @"SOFTWARE\Microsoft\Windows\CurrentVersion\CapabilityAccessManager\ConsentStore";

    // Capabilities monitoradas -> rótulo amigável do pin
    public static readonly IReadOnlyDictionary<string, string> Capabilities = new Dictionary<string, string>
    {
        ["webcam"] = "Câmera",
        ["microphone"] = "Microfone",
        ["location"] = "Localização",
        ["graphicsCaptureProgrammatic"] = "Captura de tela",
        ["graphicsCaptureWithoutBorder"] = "Captura de tela (sem borda)",
    };

    public static List<ResourceUsage> GetActiveUsages()
    {
        var running = GetRunningProcessNames();
        var result = new List<ResourceUsage>();
        foreach (var (capability, _) in Capabilities)
        {
            var apps = new List<string>();
            // HKCU cobre apps do usuário; HKLM cobre serviços/sistema
            CollectApps(Registry.CurrentUser, capability, apps, running);
            CollectApps(Registry.LocalMachine, capability, apps, running);
            if (apps.Count > 0)
                result.Add(new ResourceUsage(capability, apps.Distinct().ToList()));
        }
        return result;
    }

    /// <summary>
    /// Nomes dos processos vivos. Um processo pode morrer entre a enumeração e a
    /// leitura do nome, o que lança — por isso cada item vai em try/catch próprio.
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
            catch { /* processo morreu ou é protegido */ }
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
                // Apps desktop clássicos: uma subchave por executável (caminho com '#')
                foreach (var appName in subKey.GetSubKeyNames())
                {
                    using var appKey = subKey.OpenSubKey(appName);
                    if (!IsInUse(appKey)) continue;

                    // Órfã se o exe não existe mais OU não há processo com esse nome
                    var exePath = appName.Replace('#', '\\');
                    var procName = Path.GetFileNameWithoutExtension(exePath);
                    if (File.Exists(exePath) && running.Contains(procName))
                        apps.Add(FriendlyNameFromNonPackaged(appName));
                }
            }
            else if (IsInUse(subKey))
            {
                // Apps empacotados (Store/UWP): nome da chave é o package family name
                apps.Add(FriendlyNameFromPackaged(subName));
            }
        }
    }

    private static bool IsInUse(RegistryKey? appKey)
    {
        if (appKey is null) return false;
        var stop = appKey.GetValue("LastUsedTimeStop");
        var start = appKey.GetValue("LastUsedTimeStart");
        // Em uso: já começou alguma vez e o Stop atual é 0
        return start is long s && s > 0 && stop is long e && e == 0;
    }

    private static string FriendlyNameFromNonPackaged(string keyName)
    {
        // Ex.: "C:#Program Files#App#app.exe" -> "app.exe"
        var path = keyName.Replace('#', '\\');
        var exe = Path.GetFileName(path);
        return string.IsNullOrEmpty(exe) ? keyName : exe;
    }

    private static string FriendlyNameFromPackaged(string packageFamily)
    {
        // Ex.: "Microsoft.WindowsCamera_8wekyb3d8bbwe" -> "Microsoft.WindowsCamera"
        var idx = packageFamily.LastIndexOf('_');
        return idx > 0 ? packageFamily[..idx] : packageFamily;
    }
}

using System.IO;

namespace ResourcePins;

/// <summary>Preferences stored as key=value lines in a text file.</summary>
public static class Settings
{
    private static readonly string File_ = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "ResourcePins", "settings.txt");

    private static readonly Dictionary<string, string> _values = Load();

    private static Dictionary<string, string> Load()
    {
        var map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        try
        {
            if (File.Exists(File_))
                foreach (var line in File.ReadAllLines(File_))
                {
                    var i = line.IndexOf('=');
                    if (i > 0) map[line[..i].Trim()] = line[(i + 1)..].Trim();
                }
        }
        catch { }
        return map;
    }

    public static bool GetBool(string key, bool fallback)
        => _values.TryGetValue(key, out var v) ? v == "1" : fallback;

    public static void SetBool(string key, bool value)
    {
        _values[key] = value ? "1" : "0";
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(File_)!);
            File.WriteAllLines(File_, _values.Select(kv => $"{kv.Key}={kv.Value}"));
        }
        catch { }
    }
}

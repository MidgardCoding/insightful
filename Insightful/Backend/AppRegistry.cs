using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;

public class AppRegistry
{
    private readonly Dictionary<string, AppEntry> _byNormalizedPath = new(StringComparer.OrdinalIgnoreCase);

    public AppRegistry() { }

    public void LoadFromFile(string jsonPath)
    {
        if (!File.Exists(jsonPath)) throw new FileNotFoundException("package.json not found", jsonPath);
        string json = File.ReadAllText(jsonPath);
        var doc = JsonDocument.Parse(json);
        foreach (var prop in doc.RootElement.EnumerateObject())
        {
            var entry = JsonSerializer.Deserialize<AppEntry>(prop.Value.GetRawText());
            if (entry == null || string.IsNullOrWhiteSpace(entry.AppSrc)) continue;
            var norm = NormalizePath(entry.AppSrc);
            if (!_byNormalizedPath.ContainsKey(norm))
                _byNormalizedPath[norm] = entry;
        }
    }

    public AppEntry? FindByExePath(string exePath)
    {
        if (string.IsNullOrWhiteSpace(exePath)) return null;
        var norm = NormalizePath(exePath);
        if (_byNormalizedPath.TryGetValue(norm, out var entry)) return entry;
        var fileName = Path.GetFileName(norm);
        var match = _byNormalizedPath.Values.FirstOrDefault(e => string.Equals(Path.GetFileName(NormalizePath(e.AppSrc)), fileName, StringComparison.OrdinalIgnoreCase));
        return match;
    }

    private static string NormalizePath(string p)
    {
        try
        {
            return Path.GetFullPath(p).Trim().ToLowerInvariant();
        }
        catch
        {
            return p?.Trim().ToLowerInvariant() ?? string.Empty;
        }
    }
}
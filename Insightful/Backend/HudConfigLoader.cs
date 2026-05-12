using System.IO;
using Insightful.Model;
using Newtonsoft.Json.Linq;

namespace Insightful.Backend;

/// <summary>
/// Loads HUD config from JSON. Supports either a legacy single-app object or <c>{ "apps": [...], "default": {...} }</c>.
/// </summary>
public sealed class HudRegistry
{
    private readonly List<WindowData> _apps;
    private readonly WindowData _fallback;

    public HudRegistry(IEnumerable<WindowData> apps, WindowData fallback)
    {
        _apps = apps.Where(a => !string.IsNullOrWhiteSpace(a.AppSrc)).ToList();
        _fallback = fallback;
    }

    public WindowData ResolveForExecutable(string? activeExeFullPath)
    {
        if (string.IsNullOrWhiteSpace(activeExeFullPath))
            return _fallback;

        string normalizedActive;
        try
        {
            normalizedActive = Path.GetFullPath(activeExeFullPath);
        }
        catch
        {
            return _fallback;
        }

        foreach (var entry in _apps)
        {
            if (TryMatchExe(entry.AppSrc, normalizedActive))
                return entry;
        }

        return _fallback;
    }

    private static bool TryMatchExe(string? configuredSrc, string normalizedActive)
    {
        if (string.IsNullOrWhiteSpace(configuredSrc))
            return false;

        try
        {
            var normalizedCfg = Path.GetFullPath(configuredSrc);
            return string.Equals(normalizedCfg, normalizedActive, StringComparison.OrdinalIgnoreCase);
        }
        catch
        {
            return string.Equals(configuredSrc.Trim(), normalizedActive, StringComparison.OrdinalIgnoreCase);
        }
    }

    public static HudRegistry LoadFromFile(string filePath)
    {
        var fallback = new WindowData
        {
            AppTitle = "Empty configuration",
            AppSrc = "",
            Shortcuts =
            [
                new ShortcutItem
                {
                    Name = "Add data",
                    KeyCombination = $"Fill this file with data: {Path.GetFileName(filePath)}"
                }
            ],
            AppNotes = [
                new AppNote
                {
                    NoteTitle = "Example config",
                    NoteContent = @"{}",
                }
            ]
        };

        if (!File.Exists(filePath))
            return new HudRegistry([], fallback);

        var text = File.ReadAllText(filePath);
        List<WindowData> apps;

        try
        {
            var root = JObject.Parse(text);

            if (root["apps"] is JArray arr)
            {
                apps = arr.ToObject<List<WindowData>>() ?? [];
                if (root["default"] != null)
                {
                    var d = root["default"]!.ToObject<WindowData>();
                    if (d != null)
                        fallback = d;
                }

                return new HudRegistry(apps, fallback);
            }

            var single = root.ToObject<WindowData>();
            if (single?.AppSrc != null)
                apps = [single];
            else
                apps = [];
            return new HudRegistry(apps, fallback);
        }
        catch
        {
            var errFallback = new WindowData
            {
                AppTitle = "Parsing error",
                Shortcuts =
                [
                    new ShortcutItem { Name = "JSON", KeyCombination = filePath }
                ]
            };
            return new HudRegistry([], errFallback);
        }
    }
}

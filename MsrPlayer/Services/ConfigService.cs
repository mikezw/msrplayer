using System;
using System.IO;
using System.Text.Json;
using MsrPlayer.Models;

namespace MsrPlayer.Services;

public class ConfigService
{
    private static readonly string ConfigPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "MsrPlayer", "config.json");

    public PlayerConfig Load()
    {
        try
        {
            if (!File.Exists(ConfigPath))
            {
                return new PlayerConfig();
            }

            var json = File.ReadAllText(ConfigPath);
            return JsonSerializer.Deserialize<PlayerConfig>(json) ?? new PlayerConfig();
        }
        catch
        {
            return new PlayerConfig();
        }
    }

    public void Save(PlayerConfig config)
    {
        try
        {
            var directory = Path.GetDirectoryName(ConfigPath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            var json = JsonSerializer.Serialize(config, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(ConfigPath, json);
        }
        catch
        {
        }
    }
}

public class PlayerConfig
{
    public double Volume { get; set; } = 80;

    public PlayMode PlayMode { get; set; } = PlayMode.Sequence;

    public string CacheDirectory { get; set; } = string.Empty;

    public bool EnableCache { get; set; } = true;
}

public enum PlayMode
{
    Sequence,
    LoopOne,
    LoopAll
}
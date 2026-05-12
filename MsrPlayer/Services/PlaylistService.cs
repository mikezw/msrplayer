using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using MsrPlayer.Models;

namespace MsrPlayer.Services;

public class PlaylistService
{
    private static readonly string PlaylistPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "MsrPlayer", "playlist.json");

    public List<PlaylistItem> Load()
    {
        try
        {
            if (!File.Exists(PlaylistPath))
            {
                return new List<PlaylistItem>();
            }

            var json = File.ReadAllText(PlaylistPath);
            return JsonSerializer.Deserialize<List<PlaylistItem>>(json) ?? new List<PlaylistItem>();
        }
        catch
        {
            return new List<PlaylistItem>();
        }
    }

    public void Save(List<PlaylistItem> playlist)
    {
        try
        {
            var directory = Path.GetDirectoryName(PlaylistPath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            var json = JsonSerializer.Serialize(playlist, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(PlaylistPath, json);
        }
        catch
        {
        }
    }
}
using System;
using System.IO;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using MsrPlayer.Models;

namespace MsrPlayer.Services;

public class CacheService
{
    private readonly HttpClient _httpClient = new HttpClient();
    private readonly JsonSerializerOptions _jsonOptions = new JsonSerializerOptions { WriteIndented = true };

    private string _cacheDirectory;

    public string CacheDirectory
    {
        get { return _cacheDirectory; }
        set
        {
            _cacheDirectory = value;
            EnsureCacheDirectories();
        }
    }

    private string AudioCachePath
    {
        get { return Path.Combine(CacheDirectory, "audio"); }
    }

    private string SongCachePath
    {
        get { return Path.Combine(CacheDirectory, "songs"); }
    }

    private string LyricCachePath
    {
        get { return Path.Combine(CacheDirectory, "lyrics"); }
    }

    public CacheService()
    {
        var appDir = AppDomain.CurrentDomain.BaseDirectory;
        _cacheDirectory = Path.Combine(appDir, "cache");
        EnsureCacheDirectories();
    }

    public CacheService(string cacheDirectory)
    {
        _cacheDirectory = cacheDirectory;
        EnsureCacheDirectories();
    }

    private void EnsureCacheDirectories()
    {
        if (!Directory.Exists(AudioCachePath))
        {
            Directory.CreateDirectory(AudioCachePath);
        }

        if (!Directory.Exists(SongCachePath))
        {
            Directory.CreateDirectory(SongCachePath);
        }

        if (!Directory.Exists(LyricCachePath))
        {
            Directory.CreateDirectory(LyricCachePath);
        }
    }

    private string GetFileExtensionFromUrl(string url)
    {
        if (string.IsNullOrEmpty(url))
        {
            return "mp3";
        }

        var uri = new Uri(url);
        var fileName = Path.GetFileName(uri.LocalPath);

        if (string.IsNullOrEmpty(fileName))
        {
            return "mp3";
        }

        var extension = Path.GetExtension(fileName);
        if (string.IsNullOrEmpty(extension))
        {
            return "mp3";
        }

        return extension.TrimStart('.');
    }

    public bool HasAudioCache(string cid, string sourceUrl)
    {
        if (string.IsNullOrEmpty(cid) || string.IsNullOrEmpty(sourceUrl))
        {
            return false;
        }

        var extension = GetFileExtensionFromUrl(sourceUrl);
        var cachePath = Path.Combine(AudioCachePath, $"{cid}.{extension}");
        return File.Exists(cachePath);
    }

    public string GetAudioCachePath(string cid, string sourceUrl)
    {
        var extension = GetFileExtensionFromUrl(sourceUrl);
        return Path.Combine(AudioCachePath, $"{cid}.{extension}");
    }

    public async Task DownloadAndCacheAudio(string cid, string sourceUrl)
    {
        if (string.IsNullOrEmpty(cid) || string.IsNullOrEmpty(sourceUrl))
        {
            return;
        }

        var extension = GetFileExtensionFromUrl(sourceUrl);
        var cachePath = Path.Combine(AudioCachePath, $"{cid}.{extension}");

        var audioData = await _httpClient.GetByteArrayAsync(sourceUrl);
        await File.WriteAllBytesAsync(cachePath, audioData);
    }

    public bool HasSongDetailCache(string cid)
    {
        if (string.IsNullOrEmpty(cid))
        {
            return false;
        }

        var cachePath = Path.Combine(SongCachePath, $"{cid}.json");
        return File.Exists(cachePath);
    }

    public SongDetail? GetSongDetailCache(string cid)
    {
        if (string.IsNullOrEmpty(cid))
        {
            return null;
        }

        var cachePath = Path.Combine(SongCachePath, $"{cid}.json");

        if (!File.Exists(cachePath))
        {
            return null;
        }

        try
        {
            var json = File.ReadAllText(cachePath);
            return JsonSerializer.Deserialize<SongDetail>(json);
        }
        catch
        {
            return null;
        }
    }

    public void SaveSongDetailCache(string cid, SongDetail detail)
    {
        if (string.IsNullOrEmpty(cid) || detail == null)
        {
            return;
        }

        var cachePath = Path.Combine(SongCachePath, $"{cid}.json");
        var json = JsonSerializer.Serialize(detail, _jsonOptions);
        File.WriteAllText(cachePath, json);
    }

    public bool HasLyricCache(string cid)
    {
        if (string.IsNullOrEmpty(cid))
        {
            return false;
        }

        var cachePath = Path.Combine(LyricCachePath, $"{cid}.lrc");
        return File.Exists(cachePath);
    }

    public string? GetLyricCache(string cid)
    {
        if (string.IsNullOrEmpty(cid))
        {
            return null;
        }

        var cachePath = Path.Combine(LyricCachePath, $"{cid}.lrc");

        if (!File.Exists(cachePath))
        {
            return null;
        }

        try
        {
            return File.ReadAllText(cachePath);
        }
        catch
        {
            return null;
        }
    }

    public void SaveLyricCache(string cid, string lrcContent)
    {
        if (string.IsNullOrEmpty(cid) || string.IsNullOrEmpty(lrcContent))
        {
            return;
        }

        var cachePath = Path.Combine(LyricCachePath, $"{cid}.lrc");
        File.WriteAllText(cachePath, lrcContent);
    }

    public void DeleteAudioCache(string cid, string sourceUrl)
    {
        if (string.IsNullOrEmpty(cid) || string.IsNullOrEmpty(sourceUrl))
        {
            return;
        }

        var extension = GetFileExtensionFromUrl(sourceUrl);
        var cachePath = Path.Combine(AudioCachePath, $"{cid}.{extension}");

        if (File.Exists(cachePath))
        {
            File.Delete(cachePath);
        }
    }

    public void ClearAllCache()
    {
        if (Directory.Exists(AudioCachePath))
        {
            foreach (var file in Directory.GetFiles(AudioCachePath))
            {
                File.Delete(file);
            }
        }

        if (Directory.Exists(SongCachePath))
        {
            foreach (var file in Directory.GetFiles(SongCachePath))
            {
                File.Delete(file);
            }
        }

        if (Directory.Exists(LyricCachePath))
        {
            foreach (var file in Directory.GetFiles(LyricCachePath))
            {
                File.Delete(file);
            }
        }
    }
}
using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using MsrPlayer.Models;

namespace MsrPlayer.Services;

public class ApiService
{
    private readonly HttpClient _httpClient = new HttpClient();
    private readonly JsonSerializerOptions _jsonOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

    private const string BaseUrl = "https://monster-siren.hypergryph.com/api";

    public async Task<List<Song>> GetSongsAsync()
    {
        var response = await _httpClient.GetStringAsync($"{BaseUrl}/songs");
        var data = JsonSerializer.Deserialize<SongListResponse>(response, _jsonOptions);
        return data?.Data.List ?? new List<Song>();
    }

    public async Task<SongDetail?> GetSongDetailAsync(string cid)
    {
        var response = await _httpClient.GetStringAsync($"{BaseUrl}/song/{cid}");
        var data = JsonSerializer.Deserialize<SongDetailResponse>(response, _jsonOptions);
        return data?.Data;
    }

    public async Task<string> GetLyricAsync(string lyricUrl)
    {
        if (string.IsNullOrEmpty(lyricUrl))
        {
            return string.Empty;
        }

        try
        {
            return await _httpClient.GetStringAsync(lyricUrl);
        }
        catch
        {
            return string.Empty;
        }
    }
}
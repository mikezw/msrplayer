using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace MsrPlayer.Models;

public class SongListResponse
{
    [JsonPropertyName("data")]
    public SongListData Data { get; set; } = new SongListData();
}

public class SongListData
{
    [JsonPropertyName("list")]
    public List<Song> List { get; set; } = new List<Song>();
}

public class Song
{
    [JsonPropertyName("cid")]
    public string Cid { get; set; } = string.Empty;

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("artists")]
    public List<string> Artists { get; set; } = new List<string>();

    public string ArtistDisplay
    {
        get { return Artists.Count > 0 ? string.Join(", ", Artists) : "未知艺术家"; }
    }
}
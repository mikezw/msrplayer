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

/// <summary>
/// Runtime status of a song relative to the cached list,
/// used to highlight newly added or removed songs after a background refresh.
/// </summary>
public enum SongStatus
{
    Normal,
    New,
    Removed
}

public class Song
{
    [JsonPropertyName("cid")]
    public string Cid { get; set; } = string.Empty;

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("artists")]
    public List<string> Artists { get; set; } = new List<string>();

    /// <summary>
    /// Runtime diff marker; not persisted in the song list cache.
    /// </summary>
    [JsonIgnore]
    public SongStatus Status { get; set; } = SongStatus.Normal;

    [JsonIgnore]
    public bool IsNew => Status == SongStatus.New;

    [JsonIgnore]
    public bool IsRemoved => Status == SongStatus.Removed;

    public string ArtistDisplay
    {
        get { return Artists is { Count: > 0 } ? string.Join(", ", Artists) : string.Empty; }
    }
}

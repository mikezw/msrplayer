using System.Text.Json.Serialization;

namespace MsrPlayer.Models;

public class SongDetailResponse
{
    [JsonPropertyName("data")]
    public SongDetail Data { get; set; } = new SongDetail();
}

public class SongDetail
{
    [JsonPropertyName("cid")]
    public string Cid { get; set; } = string.Empty;

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("sourceUrl")]
    public string SourceUrl { get; set; } = string.Empty;

    [JsonPropertyName("coverUrl")]
    public string CoverUrl { get; set; } = string.Empty;

    [JsonPropertyName("lyricUrl")]
    public string LyricUrl { get; set; } = string.Empty;
}
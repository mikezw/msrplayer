using MsrPlayer.Models;
using Xunit;

namespace MsrPlayer.Tests;

public class ModelsTests
{
    [Fact]
    public void Song_ArtistDisplay_WithSingleArtist_ReturnsArtistName()
    {
        // Arrange
        var song = new Song
        {
            Cid = "1",
            Name = "Test Song",
            Artists = new List<string> { "Artist A" }
        };

        // Act
        var display = song.ArtistDisplay;

        // Assert
        Assert.Equal("Artist A", display);
    }

    [Fact]
    public void Song_ArtistDisplay_WithMultipleArtists_ReturnsCommaSeparated()
    {
        // Arrange
        var song = new Song
        {
            Cid = "1",
            Name = "Test Song",
            Artists = new List<string> { "Artist A", "Artist B", "Artist C" }
        };

        // Act
        var display = song.ArtistDisplay;

        // Assert
        Assert.Equal("Artist A, Artist B, Artist C", display);
    }

    [Fact]
    public void Song_ArtistDisplay_WithNoArtists_ReturnsUnknownArtist()
    {
        // Arrange
        var song = new Song
        {
            Cid = "1",
            Name = "Test Song",
            Artists = new List<string>()
        };

        // Act
        var display = song.ArtistDisplay;

        // Assert
        Assert.Equal("未知艺术家", display);
    }

    [Fact]
    public void PlaylistItem_Properties_CanBeSetAndRetrieved()
    {
        // Arrange & Act
        var item = new PlaylistItem
        {
            Cid = "123",
            Name = "Test Song",
            Artist = "Test Artist",
            IsPlaying = true,
            IsCached = true
        };

        // Assert
        Assert.Equal("123", item.Cid);
        Assert.Equal("Test Song", item.Name);
        Assert.Equal("Test Artist", item.Artist);
        Assert.True(item.IsPlaying);
        Assert.True(item.IsCached);
    }

    [Fact]
    public void LyricLine_Properties_CanBeSetAndRetrieved()
    {
        // Arrange & Act
        var lyric = new LyricLine
        {
            Time = TimeSpan.FromSeconds(10),
            Text = "Test lyrics"
        };

        // Assert
        Assert.Equal(TimeSpan.FromSeconds(10), lyric.Time);
        Assert.Equal("Test lyrics", lyric.Text);
    }

    [Fact]
    public void SongDetail_Properties_CanBeSetAndRetrieved()
    {
        // Arrange & Act
        var detail = new SongDetail
        {
            Cid = "123",
            Name = "Test Song",
            SourceUrl = "https://example.com/audio.wav",
            LyricUrl = "https://example.com/lyrics.lrc",
            CoverUrl = "https://example.com/cover.jpg"
        };

        // Assert
        Assert.Equal("123", detail.Cid);
        Assert.Equal("Test Song", detail.Name);
        Assert.Equal("https://example.com/audio.wav", detail.SourceUrl);
        Assert.Equal("https://example.com/lyrics.lrc", detail.LyricUrl);
        Assert.Equal("https://example.com/cover.jpg", detail.CoverUrl);
    }
}
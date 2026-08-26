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
    public void Song_ArtistDisplay_WithNoArtists_ReturnsEmptyString()
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
        Assert.Equal(string.Empty, display);
    }

    [Fact]
    public void Song_ArtistDisplay_WithNullArtists_ReturnsEmptyString()
    {
        // Arrange
        var song = new Song
        {
            Cid = "1",
            Name = "Test Song",
            Artists = null!
        };

        // Act
        var display = song.ArtistDisplay;

        // Assert
        Assert.Equal(string.Empty, display);
    }
}
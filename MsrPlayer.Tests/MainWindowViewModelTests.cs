using MsrPlayer.Models;
using MsrPlayer.ViewModels;
using Xunit;

namespace MsrPlayer.Tests;

public class MainWindowViewModelTests
{
    private static Song CreateSong(string name, params string[] artists) => new()
    {
        Cid = Guid.NewGuid().ToString(),
        Name = name,
        Artists = artists.ToList()
    };

    [Fact]
    public void SongMatches_ByName_CaseInsensitive_ReturnsTrue()
    {
        var song = CreateSong("Operation Blade", "MSR");

        Assert.True(MainWindowViewModel.SongMatches(song, "blade"));
        Assert.True(MainWindowViewModel.SongMatches(song, "BLADE"));
        Assert.True(MainWindowViewModel.SongMatches(song, "operation"));
    }

    [Fact]
    public void SongMatches_ByArtist_CaseInsensitive_ReturnsTrue()
    {
        var song = CreateSong("Some Song", "塞壬唱片-MSR");

        Assert.True(MainWindowViewModel.SongMatches(song, "塞壬"));
        Assert.True(MainWindowViewModel.SongMatches(song, "msr"));
    }

    [Fact]
    public void SongMatches_NoMatch_ReturnsFalse()
    {
        var song = CreateSong("Requiem", "MSR");

        Assert.False(MainWindowViewModel.SongMatches(song, "nonexistent"));
        Assert.False(MainWindowViewModel.SongMatches(song, "lullaby"));
    }

    [Fact]
    public void SongMatches_NullName_DoesNotThrowAndCanMatchByArtist()
    {
        var song = CreateSong(null!, "MSR");

        Assert.False(MainWindowViewModel.SongMatches(song, "anything"));
        Assert.True(MainWindowViewModel.SongMatches(song, "msr"));
    }

    [Fact]
    public void SongMatches_NullArtists_DoesNotThrowAndCanMatchByName()
    {
        var song = new Song
        {
            Cid = "1",
            Name = "Operation Blade",
            Artists = null!
        };

        Assert.True(MainWindowViewModel.SongMatches(song, "blade"));
        Assert.False(MainWindowViewModel.SongMatches(song, "unknown-artist"));
    }
}
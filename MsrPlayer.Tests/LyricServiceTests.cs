using MsrPlayer.Services;
using Xunit;

namespace MsrPlayer.Tests;

public class LyricServiceTests
{
    private readonly LyricService _lyricService;

    public LyricServiceTests()
    {
        _lyricService = new LyricService();
    }

    [Fact]
    public void ParseLrc_WithValidLrc_ReturnsParsedLyrics()
    {
        // Arrange
        var lrcContent = "[00:01.00]第一行歌词\n[00:05.00]第二行歌词\n[00:10.00]第三行歌词";

        // Act
        var result = _lyricService.ParseLrc(lrcContent);

        // Assert
        Assert.Equal(3, result.Count);
        Assert.Equal("第一行歌词", result[0].Text);
        Assert.Equal("第二行歌词", result[1].Text);
        Assert.Equal("第三行歌词", result[2].Text);
    }

    [Fact]
    public void ParseLrc_WithEmptyString_ReturnsEmptyList()
    {
        // Arrange
        var lrcContent = "";

        // Act
        var result = _lyricService.ParseLrc(lrcContent);

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public void ParseLrc_WithNullString_ReturnsEmptyList()
    {
        // Arrange
        string? lrcContent = null;

        // Act
        var result = _lyricService.ParseLrc(lrcContent!);

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public void ParseLrc_WithInvalidLines_SkipsInvalidLines()
    {
        // Arrange
        var lrcContent = "[00:01.00]有效歌词\n无效行\n[00:05.00]另一行有效歌词";

        // Act
        var result = _lyricService.ParseLrc(lrcContent);

        // Assert
        Assert.Equal(2, result.Count);
        Assert.Equal("有效歌词", result[0].Text);
        Assert.Equal("另一行有效歌词", result[1].Text);
    }

    [Fact]
    public void GetCurrentLyricIndex_WithMatchingTime_ReturnsCorrectIndex()
    {
        // Arrange
        var lyrics = _lyricService.ParseLrc("[00:00.00]第一行\n[00:05.00]第二行\n[00:10.00]第三行");
        var currentTime = TimeSpan.FromSeconds(7);

        // Act
        var index = _lyricService.GetCurrentLyricIndex(lyrics, currentTime);

        // Assert
        Assert.Equal(1, index); // 应该是第二行（索引1）
    }

    [Fact]
    public void GetCurrentLyricIndex_WithTimeBeforeFirstLine_ReturnsZero()
    {
        // Arrange
        var lyrics = _lyricService.ParseLrc("[00:05.00]第一行\n[00:10.00]第二行");
        var currentTime = TimeSpan.FromSeconds(2);

        // Act
        var index = _lyricService.GetCurrentLyricIndex(lyrics, currentTime);

        // Assert
        Assert.Equal(0, index);
    }

    [Fact]
    public void GetCurrentLyricIndex_WithEmptyList_ReturnsMinusOne()
    {
        // Arrange
        var lyrics = new List<Models.LyricLine>();
        var currentTime = TimeSpan.FromSeconds(5);

        // Act
        var index = _lyricService.GetCurrentLyricIndex(lyrics, currentTime);

        // Assert
        Assert.Equal(-1, index);
    }
}
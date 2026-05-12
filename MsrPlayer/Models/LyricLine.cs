using System;

namespace MsrPlayer.Models;

public class LyricLine
{
    public TimeSpan Time { get; set; }

    public string Text { get; set; } = string.Empty;
}
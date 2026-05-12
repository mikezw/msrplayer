using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using MsrPlayer.Models;

namespace MsrPlayer.Services;

public class LyricService
{
    public List<LyricLine> ParseLrc(string lrcContent)
    {
        var lines = new List<LyricLine>();

        if (string.IsNullOrEmpty(lrcContent))
        {
            return lines;
        }

        var regex = new Regex(@"\[(\d{2}):(\d{2})\.(\d{2,3})\](.*)");

        foreach (var line in lrcContent.Split('\n'))
        {
            var trimmedLine = line.Trim();

            if (string.IsNullOrEmpty(trimmedLine))
            {
                continue;
            }

            var match = regex.Match(trimmedLine);

            if (match.Success)
            {
                var minutes = int.Parse(match.Groups[1].Value);
                var seconds = int.Parse(match.Groups[2].Value);
                var milliseconds = int.Parse(match.Groups[3].Value) * (match.Groups[3].Value.Length == 2 ? 10 : 1);
                var text = match.Groups[4].Value.Trim();

                if (string.IsNullOrEmpty(text))
                {
                    continue;
                }

                if (text.StartsWith("ti:") || text.StartsWith("ar:") || text.StartsWith("al:") || text.StartsWith("by:"))
                {
                    continue;
                }

                var time = TimeSpan.FromMinutes(minutes) + TimeSpan.FromSeconds(seconds) + TimeSpan.FromMilliseconds(milliseconds);
                lines.Add(new LyricLine { Time = time, Text = text });
            }
        }

        lines.Sort((a, b) => a.Time.CompareTo(b.Time));
        return lines;
    }

    public int GetCurrentLyricIndex(List<LyricLine> lyrics, TimeSpan currentTime)
    {
        if (lyrics == null || lyrics.Count == 0)
        {
            return -1;
        }

        if (currentTime < lyrics[0].Time)
        {
            return 0;
        }

        for (int i = 0; i < lyrics.Count; i++)
        {
            if (lyrics[i].Time > currentTime)
            {
                return i - 1;
            }
        }

        return lyrics.Count - 1;
    }
}
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MsrPlayer.Models;
using MsrPlayer.Services;

namespace MsrPlayer.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    private readonly ApiService _apiService;
    private readonly AudioService _audioService;
    private readonly PlaylistService _playlistService;
    private readonly ConfigService _configService;
    private readonly LyricService _lyricService;
    private readonly CacheService _cacheService;
    private PlayerConfig _config;
    private List<LyricLine> _currentLyrics = new List<LyricLine>();
    private int _currentLyricIndex = -1;
    private SongDetail? _currentSongDetail;

    [ObservableProperty]
    private ObservableCollection<Song> _songs = new ObservableCollection<Song>();

    [ObservableProperty]
    private ObservableCollection<PlaylistItem> _playlist = new ObservableCollection<PlaylistItem>();

    [ObservableProperty]
    private int _currentIndex = -1;

    [ObservableProperty]
    private string _currentSongName = string.Empty;

    [ObservableProperty]
    private string _statusText = "加载中...";

    [ObservableProperty]
    private bool _isPlaying;

    [ObservableProperty]
    private double _volume;

    [ObservableProperty]
    private PlayMode _currentMode;

    [ObservableProperty]
    private double _currentPosition;

    [ObservableProperty]
    private string _currentTimeText = "00:00";

    [ObservableProperty]
    private string _totalTimeText = "00:00";

    [ObservableProperty]
    private ObservableCollection<LyricLine> _lyrics = new ObservableCollection<LyricLine>();

    [ObservableProperty]
    private string _currentLyricText = string.Empty;

    [ObservableProperty]
    private bool _hasLyrics;

    [ObservableProperty]
    private bool _enableCache;

    public string LoopModeText
    {
        get
        {
            return CurrentMode switch
            {
                PlayMode.Sequence => "顺序",
                PlayMode.LoopOne => "单曲循环",
                PlayMode.LoopAll => "列表循环",
                _ => "顺序"
            };
        }
    }

    public string CacheModeText
    {
        get
        {
            return EnableCache ? "缓存已开启" : "缓存已关闭";
        }
    }

    public MainWindowViewModel(
        ApiService apiService,
        AudioService audioService,
        PlaylistService playlistService,
        ConfigService configService,
        LyricService lyricService,
        CacheService cacheService)
    {
        _apiService = apiService;
        _audioService = audioService;
        _playlistService = playlistService;
        _configService = configService;
        _lyricService = lyricService;
        _cacheService = cacheService;

        _config = _configService.Load();
        _volume = _config.Volume;
        _currentMode = _config.PlayMode;
        _enableCache = _config.EnableCache;

        if (!string.IsNullOrEmpty(_config.CacheDirectory))
        {
            _cacheService.CacheDirectory = _config.CacheDirectory;
        }

        _audioService.PlaybackEnded += OnPlaybackEnded;
        _audioService.StateChanged += OnStateChanged;
        _audioService.PositionChanged += OnPositionChanged;
        _audioService.Volume = (float)(Volume / 100f);

        LoadDataAsync();
    }

    partial void OnVolumeChanged(double value)
    {
        _audioService.Volume = (float)(value / 100f);
        _config.Volume = value;
        _configService.Save(_config);
    }

    partial void OnCurrentModeChanged(PlayMode value)
    {
        _config.PlayMode = value;
        _configService.Save(_config);
        OnPropertyChanged(nameof(LoopModeText));
    }

    partial void OnEnableCacheChanged(bool value)
    {
        _config.EnableCache = value;
        _configService.Save(_config);
        OnPropertyChanged(nameof(CacheModeText));
        StatusText = value ? "缓存模式已开启" : "缓存模式已关闭";
    }

    [RelayCommand]
    private void ToggleCacheMode()
    {
        EnableCache = !EnableCache;
    }

    public void SetCacheDirectory(string path)
    {
        _config.CacheDirectory = path;
        _configService.Save(_config);
        _cacheService.CacheDirectory = path;
        UpdateCacheStatus();
        StatusText = $"缓存目录已设置为: {path}";
    }

    private string FormatTime(TimeSpan time)
    {
        return $"{(int)time.TotalMinutes:D2}:{time.Seconds:D2}";
    }

    private void UpdateTimeText()
    {
        CurrentTimeText = FormatTime(_audioService.CurrentTime);
        TotalTimeText = FormatTime(_audioService.TotalTime);
    }

    private void OnPositionChanged(object? sender, EventArgs e)
    {
        Dispatcher.UIThread.Post(() =>
        {
            if (_audioService.TotalTime > TimeSpan.Zero)
            {
                CurrentPosition = _audioService.CurrentTime.TotalMilliseconds / _audioService.TotalTime.TotalMilliseconds * 100;
            }
            UpdateTimeText();
            UpdateCurrentLyric();
        });
    }

    private void UpdateCurrentLyric()
    {
        if (_currentLyrics.Count == 0)
        {
            return;
        }

        var currentTime = _audioService.CurrentTime;
        var newIndex = _lyricService.GetCurrentLyricIndex(_currentLyrics, currentTime);

        if (newIndex >= 0 && newIndex < _currentLyrics.Count)
        {
            _currentLyricIndex = newIndex;
            CurrentLyricText = _currentLyrics[newIndex].Text;
        }
    }

    private void UpdatePlayingIndicator(int newIndex)
    {
        for (int i = 0; i < Playlist.Count; i++)
        {
            Playlist[i].IsPlaying = (i == newIndex);
        }
    }

    private async void LoadDataAsync()
    {
        try
        {
            var songs = await _apiService.GetSongsAsync();
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                Songs.Clear();
                foreach (var song in songs)
                {
                    Songs.Add(song);
                }
                StatusText = $"共 {Songs.Count} 歌曲";
            });

            var savedPlaylist = _playlistService.Load();
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                Playlist.Clear();
                foreach (var item in savedPlaylist)
                {
                    Playlist.Add(item);
                }
                UpdateCacheStatus();
                StatusText = $"共 {Songs.Count} 歌曲，播放列表 {Playlist.Count} 首";
            });
        }
        catch (Exception ex)
        {
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                StatusText = $"加载失败: {ex.Message}";
            });
        }
    }

    private void UpdateCacheStatus()
    {
        for (int i = 0; i < Playlist.Count; i++)
        {
            var item = Playlist[i];
            if (_cacheService.HasSongDetailCache(item.Cid))
            {
                var detail = _cacheService.GetSongDetailCache(item.Cid);
                if (detail != null && !string.IsNullOrEmpty(detail.SourceUrl))
                {
                    item.IsCached = _cacheService.HasAudioCache(item.Cid, detail.SourceUrl);
                }
            }
            else
            {
                item.IsCached = false;
            }
        }
    }

    [RelayCommand]
    private void AddToPlaylist(Song? song)
    {
        if (song == null)
        {
            return;
        }

        if (Playlist.Any(p => p.Cid == song.Cid))
        {
            return;
        }

        var newItem = new PlaylistItem
        {
            Cid = song.Cid,
            Name = song.Name,
            Artist = song.ArtistDisplay,
            IsPlaying = false,
            IsCached = false
        };

        Playlist.Add(newItem);
        SavePlaylist();
        StatusText = $"已添加 {song.Name}，播放列表 {Playlist.Count} 首";
    }

    private async Task<SongDetail?> GetSongDetailWithCache(string cid)
    {
        if (_cacheService.HasSongDetailCache(cid))
        {
            var cachedDetail = _cacheService.GetSongDetailCache(cid);
            if (cachedDetail != null)
            {
                return cachedDetail;
            }
        }

        var detail = await _apiService.GetSongDetailAsync(cid);
        if (detail != null)
        {
            _cacheService.SaveSongDetailCache(cid, detail);
        }

        return detail;
    }

    private async Task<string> GetLyricWithCache(string cid, string lyricUrl)
    {
        if (_cacheService.HasLyricCache(cid))
        {
            var cachedLyric = _cacheService.GetLyricCache(cid);
            if (!string.IsNullOrEmpty(cachedLyric))
            {
                return cachedLyric;
            }
        }

        var lrcContent = await _apiService.GetLyricAsync(lyricUrl);
        if (!string.IsNullOrEmpty(lrcContent))
        {
            _cacheService.SaveLyricCache(cid, lrcContent);
        }

        return lrcContent;
    }

    private async Task PlaySongAtIndex(int index)
    {
        if (index < 0 || index >= Playlist.Count)
        {
            return;
        }

        var item = Playlist[index];
        string songName = item.Name;

        await Dispatcher.UIThread.InvokeAsync(() =>
        {
            CurrentIndex = index;
            CurrentSongName = songName;
            StatusText = $"正在获取 {songName}...";
            UpdatePlayingIndicator(index);
            CurrentPosition = 0;
            CurrentTimeText = "00:00";
            TotalTimeText = "00:00";
            Lyrics.Clear();
            CurrentLyricText = string.Empty;
            HasLyrics = false;
            _currentLyrics.Clear();
            _currentLyricIndex = -1;
        });

        try
        {
            var detail = await GetSongDetailWithCache(item.Cid);

            if (detail == null || string.IsNullOrEmpty(detail.SourceUrl))
            {
                await Dispatcher.UIThread.InvokeAsync(() =>
                {
                    StatusText = "无法获取播放地址";
                });
                return;
            }

            _currentSongDetail = detail;

            if (!string.IsNullOrEmpty(detail.LyricUrl))
            {
                try
                {
                    var lrcContent = await GetLyricWithCache(item.Cid, detail.LyricUrl);

                    if (!string.IsNullOrEmpty(lrcContent))
                    {
                        var parsedLyrics = _lyricService.ParseLrc(lrcContent);

                        await Dispatcher.UIThread.InvokeAsync(() =>
                        {
                            _currentLyrics = parsedLyrics;
                            Lyrics.Clear();
                            foreach (var line in parsedLyrics)
                            {
                                Lyrics.Add(line);
                            }
                            HasLyrics = Lyrics.Count > 0;
                            if (Lyrics.Count > 0)
                            {
                                CurrentLyricText = Lyrics[0].Text;
                            }
                        });
                    }
                }
                catch
                {
                }
            }

            string playUrl;
            if (_cacheService.HasAudioCache(item.Cid, detail.SourceUrl))
            {
                playUrl = _cacheService.GetAudioCachePath(item.Cid, detail.SourceUrl);
                await Dispatcher.UIThread.InvokeAsync(() =>
                {
                    item.IsCached = true;
                    StatusText = $"正在播放: {songName} (缓存)";
                });
            }
            else
            {
                playUrl = detail.SourceUrl;
                await Dispatcher.UIThread.InvokeAsync(() =>
                {
                    StatusText = EnableCache ? $"正在播放: {songName} (边下载边播放)" : $"正在播放: {songName}";
                });

                if (EnableCache)
                {
                    _ = Task.Run(async () =>
                    {
                        try
                        {
                            await _cacheService.DownloadAndCacheAudio(item.Cid, detail.SourceUrl);
                            await Dispatcher.UIThread.InvokeAsync(() =>
                            {
                                item.IsCached = true;
                            });
                        }
                        catch
                        {
                        }
                    });
                }
            }

            await _audioService.PlayAsync(playUrl);
        }
        catch (Exception ex)
        {
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                StatusText = $"播放失败: {ex.Message}";
            });
        }
    }

    [RelayCommand]
    private async Task PlayItem(PlaylistItem? item)
    {
        if (item == null)
        {
            return;
        }

        var index = Playlist.IndexOf(item);
        if (index >= 0)
        {
            await PlaySongAtIndex(index);
        }
    }

    [RelayCommand]
    private void RemoveItem(PlaylistItem? item)
    {
        if (item == null)
        {
            return;
        }

        var index = Playlist.IndexOf(item);
        if (index < 0)
        {
            return;
        }

        bool wasPlaying = item.IsPlaying;
        Playlist.RemoveAt(index);

        if (wasPlaying)
        {
            _audioService.Stop();
            CurrentIndex = -1;
            CurrentSongName = string.Empty;
            UpdatePlayingIndicator(-1);
            CurrentPosition = 0;
            CurrentTimeText = "00:00";
            TotalTimeText = "00:00";
        }
        else if (index < CurrentIndex)
        {
            CurrentIndex--;
        }

        SavePlaylist();
        StatusText = $"播放列表 {Playlist.Count} 首";
    }

    [RelayCommand]
    private async Task UpdateCache(PlaylistItem? item)
    {
        if (item == null)
        {
            return;
        }

        await Dispatcher.UIThread.InvokeAsync(() =>
        {
            StatusText = $"正在更新缓存: {item.Name}...";
        });

        try
        {
            var detail = await _apiService.GetSongDetailAsync(item.Cid);

            if (detail == null || string.IsNullOrEmpty(detail.SourceUrl))
            {
                await Dispatcher.UIThread.InvokeAsync(() =>
                {
                    StatusText = "无法获取播放地址";
                });
                return;
            }

            _cacheService.SaveSongDetailCache(item.Cid, detail);

            if (!string.IsNullOrEmpty(detail.LyricUrl))
            {
                var lrcContent = await _apiService.GetLyricAsync(detail.LyricUrl);
                if (!string.IsNullOrEmpty(lrcContent))
                {
                    _cacheService.SaveLyricCache(item.Cid, lrcContent);
                }
            }

            var oldSourceUrl = _cacheService.GetSongDetailCache(item.Cid)?.SourceUrl ?? string.Empty;
            if (!string.IsNullOrEmpty(oldSourceUrl))
            {
                _cacheService.DeleteAudioCache(item.Cid, oldSourceUrl);
            }

            await _cacheService.DownloadAndCacheAudio(item.Cid, detail.SourceUrl);

            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                item.IsCached = true;
                StatusText = $"缓存已更新: {item.Name}";
            });
        }
        catch (Exception ex)
        {
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                StatusText = $"更新缓存失败: {ex.Message}";
            });
        }
    }

    [RelayCommand]
    private async Task TogglePlay()
    {
        if (_audioService.State == PlaybackState.Playing)
        {
            _audioService.Pause();
        }
        else if (_audioService.State == PlaybackState.Paused)
        {
            _audioService.Resume();
        }
        else if (Playlist.Count > 0)
        {
            await PlaySongAtIndex(CurrentIndex < 0 ? 0 : CurrentIndex);
        }
    }

    [RelayCommand]
    private async Task Next()
    {
        if (Playlist.Count == 0)
        {
            return;
        }

        int nextIndex;
        if (CurrentIndex < Playlist.Count - 1)
        {
            nextIndex = CurrentIndex + 1;
        }
        else if (CurrentMode == PlayMode.LoopAll)
        {
            nextIndex = 0;
        }
        else
        {
            return;
        }

        await PlaySongAtIndex(nextIndex);
    }

    [RelayCommand]
    private async Task Prev()
    {
        if (Playlist.Count == 0)
        {
            return;
        }

        int prevIndex;
        if (CurrentIndex > 0)
        {
            prevIndex = CurrentIndex - 1;
        }
        else if (CurrentMode == PlayMode.LoopAll)
        {
            prevIndex = Playlist.Count - 1;
        }
        else
        {
            return;
        }

        await PlaySongAtIndex(prevIndex);
    }

    [RelayCommand]
    private void ToggleLoopMode()
    {
        CurrentMode = CurrentMode switch
        {
            PlayMode.Sequence => PlayMode.LoopOne,
            PlayMode.LoopOne => PlayMode.LoopAll,
            PlayMode.LoopAll => PlayMode.Sequence,
            _ => PlayMode.Sequence
        };
        StatusText = $"播放模式: {LoopModeText}";
    }

    private void SavePlaylist()
    {
        var itemsToSave = Playlist.Select(p => new PlaylistItem
        {
            Cid = p.Cid,
            Name = p.Name,
            Artist = p.Artist
        }).ToList();
        _playlistService.Save(itemsToSave);
    }

    private async void OnPlaybackEnded(object? sender, EventArgs e)
    {
        if (CurrentMode == PlayMode.LoopOne && CurrentIndex >= 0)
        {
            await PlaySongAtIndex(CurrentIndex);
            return;
        }

        if (CurrentIndex < Playlist.Count - 1)
        {
            await PlaySongAtIndex(CurrentIndex + 1);
        }
        else if (CurrentMode == PlayMode.LoopAll)
        {
            await PlaySongAtIndex(0);
        }
        else
        {
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                CurrentIndex = -1;
                CurrentSongName = string.Empty;
                UpdatePlayingIndicator(-1);
                CurrentPosition = 0;
                CurrentTimeText = "00:00";
                TotalTimeText = "00:00";
                StatusText = "播放完成";
            });
        }
    }

    private void OnStateChanged(object? sender, PlaybackState state)
    {
        Dispatcher.UIThread.Post(() =>
        {
            IsPlaying = state == PlaybackState.Playing;
        });
    }
}
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MsrPlayer.Models;
using MsrPlayer.Services;
using Velopack;
using Velopack.Exceptions;

namespace MsrPlayer.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    private readonly ApiService _apiService;
    private readonly AudioService _audioService;
    private readonly PlaylistService _playlistService;
    private readonly ConfigService _configService;
    private readonly LyricService _lyricService;
    private readonly CacheService _cacheService;
    private readonly UpdateService _updateService;
    private readonly ILocalizationService _localizationService;
    private PlayerConfig _config;
    private List<LyricLine> _currentLyrics = new List<LyricLine>();
    private int _currentLyricIndex = -1;
    private SongDetail? _currentSongDetail;
    private List<Song> _allSongs = new List<Song>();
    private UpdateInfo? _updateInfo;

    [ObservableProperty]
    private string _searchText = string.Empty;

    [ObservableProperty]
    private ObservableCollection<Song> _songs = new ObservableCollection<Song>();

    [ObservableProperty]
    private ObservableCollection<PlaylistItem> _playlist = new ObservableCollection<PlaylistItem>();

    [ObservableProperty]
    private int _currentIndex = -1;

    [ObservableProperty]
    private string _currentSongName = string.Empty;

    [ObservableProperty]
    private string _statusText = string.Empty;

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

    [ObservableProperty]
    private bool _isUpdateVisible;

    [ObservableProperty]
    private string _updateBannerText = string.Empty;

    [ObservableProperty]
    private string _updateButtonText = string.Empty;

    [ObservableProperty]
    private bool _isUpdating;

    [ObservableProperty]
    private double _updateProgress;

    #region Localized UI text properties

    public string SongListTitle => _localizationService["Common_SongList"];

    public string DoubleClickToAddHint => _localizationService["Common_DoubleClickToAdd"];

    public string SearchPlaceholder => _localizationService["Common_SearchPlaceholder"];

    public string NowPlayingTitle => _localizationService["Common_NowPlaying"];

    public string LyricsTitle => _localizationService["Common_Lyrics"];

    public string PlaylistTitle => _localizationService["Common_Playlist"];

    public string PlaylistHint => _localizationService["Common_PlaylistHint"];

    public string LanguageButtonText => _localizationService["Common_LanguageButton"];

    public string SelectCacheDirectoryTitle => _localizationService["Common_SelectCacheDirectory"];

    #endregion

    public string LoopModeText
    {
        get
        {
            return CurrentMode switch
            {
                PlayMode.Sequence => _localizationService["Player_LoopSequence"],
                PlayMode.LoopOne => _localizationService["Player_LoopOne"],
                PlayMode.LoopAll => _localizationService["Player_LoopAll"],
                _ => _localizationService["Player_LoopSequence"]
            };
        }
    }

    public string CacheModeText
    {
        get
        {
            return EnableCache
                ? _localizationService["Player_CacheEnabled"]
                : _localizationService["Player_CacheDisabled"];
        }
    }

    public MainWindowViewModel(
        ApiService apiService,
        AudioService audioService,
        PlaylistService playlistService,
        ConfigService configService,
        LyricService lyricService,
        CacheService cacheService,
        UpdateService updateService,
        ILocalizationService localizationService)
    {
        _apiService = apiService;
        _audioService = audioService;
        _playlistService = playlistService;
        _configService = configService;
        _lyricService = lyricService;
        _cacheService = cacheService;
        _updateService = updateService;
        _localizationService = localizationService;

        _config = _configService.Load();
        _volume = _config.Volume;
        _currentMode = _config.PlayMode;
        _enableCache = _config.EnableCache;

        _localizationService.LanguageChanged += OnLanguageChanged;
        _localizationService.ChangeLanguage(_config.Language);
        RefreshLocalizedTexts();

        if (!string.IsNullOrEmpty(_config.CacheDirectory))
        {
            _cacheService.CacheDirectory = _config.CacheDirectory;
        }

        _audioService.PlaybackEnded += OnPlaybackEnded;
        _audioService.StateChanged += OnStateChanged;
        _audioService.PositionChanged += OnPositionChanged;
        _audioService.Volume = (float)(Volume / 100f);

        LoadDataAsync();
        _ = CheckForUpdateAsync(silent: true);
    }

    private void OnLanguageChanged(object? sender, EventArgs e)
    {
        Dispatcher.UIThread.Post(RefreshLocalizedTexts);
    }

    private void RefreshLocalizedTexts()
    {
        OnPropertyChanged(nameof(LoopModeText));
        OnPropertyChanged(nameof(CacheModeText));
        OnPropertyChanged(nameof(SongListTitle));
        OnPropertyChanged(nameof(DoubleClickToAddHint));
        OnPropertyChanged(nameof(SearchPlaceholder));
        OnPropertyChanged(nameof(NowPlayingTitle));
        OnPropertyChanged(nameof(LyricsTitle));
        OnPropertyChanged(nameof(PlaylistTitle));
        OnPropertyChanged(nameof(PlaylistHint));
        OnPropertyChanged(nameof(LanguageButtonText));
    }

    [RelayCommand]
    private void ToggleLanguage()
    {
        var newLanguage = _config.Language == AppLanguage.English
            ? AppLanguage.ChineseSimplified
            : AppLanguage.English;

        _localizationService.ChangeLanguage(newLanguage);
        _config.Language = newLanguage;
        _configService.Save(_config);
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
        StatusText = value
            ? _localizationService["Status_CacheModeEnabled"]
            : _localizationService["Status_CacheModeDisabled"];
    }

    partial void OnSearchTextChanged(string value)
    {
        FilterSongs();
    }

    internal static bool SongMatches(Song song, string searchText)
    {
        return (!string.IsNullOrEmpty(song.Name) && song.Name.Contains(searchText, StringComparison.OrdinalIgnoreCase))
            || song.ArtistDisplay.Contains(searchText, StringComparison.OrdinalIgnoreCase);
    }

    private void FilterSongs()
    {
        if (string.IsNullOrWhiteSpace(SearchText))
        {
            Songs = new ObservableCollection<Song>(_allSongs);
        }
        else
        {
            var filtered = _allSongs.Where(s => SongMatches(s, SearchText)).ToList();
            Songs = new ObservableCollection<Song>(filtered);
        }

        StatusText = string.IsNullOrWhiteSpace(SearchText)
            ? _localizationService.Format("Status_TotalSongs", _allSongs.Count)
            : _localizationService.Format("Status_SearchResult", Songs.Count, _allSongs.Count);
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
        StatusText = _localizationService.Format("Status_CacheDirectorySet", path);
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
            // Step 1: Show the cached song list immediately for fast startup
            var cachedSongs = _cacheService.GetSongListCache();
            if (cachedSongs is { Count: > 0 })
            {
                await Dispatcher.UIThread.InvokeAsync(() =>
                {
                    _allSongs = cachedSongs;
                    FilterSongs();
                    StatusText = _localizationService["Status_LoadingCachedList"];
                });
            }

            // Step 2: Load the saved playlist from local storage
            var savedPlaylist = _playlistService.Load();
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                Playlist.Clear();
                foreach (var item in savedPlaylist)
                {
                    Playlist.Add(item);
                }
                UpdateCacheStatus();
            });

            // Step 3: Fetch the latest song list in the background and diff it
            await RefreshSongListAsync();
        }
        catch (Exception ex)
        {
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                StatusText = _localizationService.Format("Status_LoadFailed", ex.Message);
            });
        }
    }

    /// <summary>
    /// Fetches the latest song list from the API, caches it, and marks
    /// newly added songs and songs removed from the store.
    /// </summary>
    private async Task RefreshSongListAsync()
    {
        try
        {
            var latestSongs = await _apiService.GetSongsAsync();
            if (latestSongs.Count == 0)
            {
                return;
            }

            var hasExistingList = _allSongs.Count > 0;
            var latestCids = new HashSet<string>(latestSongs.Select(s => s.Cid));
            int newCount = 0;
            int removedCount = 0;

            if (hasExistingList)
            {
                var cachedCids = new HashSet<string>(_allSongs.Select(s => s.Cid));

                // Mark songs that are not in the cached list as new
                foreach (var song in latestSongs)
                {
                    if (!cachedCids.Contains(song.Cid))
                    {
                        song.Status = SongStatus.New;
                        newCount++;
                    }
                }

                // Keep songs that disappeared from the latest list and mark them as removed.
                // Songs without playable cache are flagged as unavailable and cannot be added.
                var removedSongs = _allSongs
                    .Where(s => !latestCids.Contains(s.Cid))
                    .Select(s =>
                    {
                        s.Status = SongStatus.Removed;
                        s.IsUnavailable = !HasPlayableCache(s.Cid);
                        return s;
                    })
                    .ToList();
                removedCount = removedSongs.Count;

                latestSongs.AddRange(removedSongs);
            }

            // Cache only the clean latest list so removed songs do not reappear on next startup
            _cacheService.SaveSongListCache(latestSongs.Where(s => s.Status != SongStatus.Removed).ToList());

            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                _allSongs = latestSongs;
                FilterSongs();

                StatusText = hasExistingList && (newCount > 0 || removedCount > 0)
                    ? _localizationService.Format("Status_ListUpdated", newCount, removedCount)
                    : _localizationService.Format("Status_TotalSongsWithPlaylist", _allSongs.Count, Playlist.Count);
            });
        }
        catch (Exception ex)
        {
            // A failed background refresh should not break the cached list already shown
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                StatusText = _localizationService.Format("Status_RefreshFailed", ex.Message);
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

    /// <summary>
    /// Returns true when a song has both a cached detail (with playback URL)
    /// and cached audio, so it remains playable even if removed from the store.
    /// </summary>
    private bool HasPlayableCache(string cid)
    {
        if (!_cacheService.HasSongDetailCache(cid))
        {
            return false;
        }

        var detail = _cacheService.GetSongDetailCache(cid);
        return detail != null
            && !string.IsNullOrEmpty(detail.SourceUrl)
            && _cacheService.HasAudioCache(cid, detail.SourceUrl);
    }

    [RelayCommand]
    private void AddToPlaylist(Song? song)
    {
        if (song == null)
        {
            return;
        }

        // Removed songs without playable cache cannot be added
        if (song.IsUnavailable)
        {
            StatusText = _localizationService["Status_SongUnavailable"];
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
            Artist = string.IsNullOrEmpty(song.ArtistDisplay)
                ? _localizationService["Common_UnknownArtist"]
                : song.ArtistDisplay,
            IsPlaying = false,
            IsCached = false
        };

        Playlist.Add(newItem);
        SavePlaylist();
        StatusText = _localizationService.Format("Status_AddedToPlaylist", song.Name, Playlist.Count);
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
            StatusText = _localizationService.Format("Status_FetchingSong", songName);
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
                    StatusText = _localizationService["Status_NoPlaybackUrl"];
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
                    StatusText = _localizationService.Format("Status_PlayingCached", songName);
                });
            }
            else
            {
                playUrl = detail.SourceUrl;
                await Dispatcher.UIThread.InvokeAsync(() =>
                {
                    StatusText = EnableCache
                        ? _localizationService.Format("Status_PlayingStreamAndCache", songName)
                        : _localizationService.Format("Status_PlayingStreaming", songName);
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
                StatusText = _localizationService.Format("Status_PlayFailed", ex.Message);
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
        StatusText = _localizationService.Format("Status_PlaylistCount", Playlist.Count);
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
            StatusText = _localizationService.Format("Status_UpdatingCache", item.Name);
        });

        try
        {
            var detail = await _apiService.GetSongDetailAsync(item.Cid);

            if (detail == null || string.IsNullOrEmpty(detail.SourceUrl))
            {
                await Dispatcher.UIThread.InvokeAsync(() =>
                {
                    StatusText = _localizationService["Status_NoPlaybackUrl"];
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
                StatusText = _localizationService.Format("Status_CacheUpdated", item.Name);
            });
        }
        catch (Exception ex)
        {
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                StatusText = _localizationService.Format("Status_CacheUpdateFailed", ex.Message);
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
        StatusText = _localizationService.Format("Status_PlayMode", LoopModeText);
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
                StatusText = _localizationService["Status_PlaybackComplete"];
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

    private async Task CheckForUpdateAsync(bool silent)
    {
        if (!silent)
        {
            UpdateBannerText = _localizationService["Update_Checking"];
            IsUpdateVisible = true;
        }

        try
        {
            var info = await _updateService.CheckForUpdatesAsync();

            if (info == null)
            {
                IsUpdateVisible = false;
                if (!silent)
                {
                    StatusText = _localizationService["Update_UpToDate"];
                }
                return;
            }

            var currentVersion = Assembly.GetExecutingAssembly().GetName().Version?.ToString();
            var latestVersion = info.TargetFullRelease.Version.ToString();
            if (!UpdateService.IsNewerVersion(currentVersion, latestVersion))
            {
                IsUpdateVisible = false;
                if (!silent)
                {
                    StatusText = _localizationService["Update_UpToDate"];
                }
                return;
            }

            _updateInfo = info;
            UpdateBannerText = _localizationService.Format("Update_NewVersionFound", latestVersion);
            UpdateButtonText = _localizationService["Update_DownloadButton"];
            IsUpdateVisible = true;
            if (!silent)
            {
                StatusText = _localizationService.Format("Update_FoundClickToDownload", latestVersion);
            }
        }
        catch (NotInstalledException)
        {
            // Non-installed build (running from bin/publish directory without Velopack install records): cannot check for updates
            IsUpdateVisible = false;
            if (!silent)
            {
                StatusText = _localizationService["Update_NotInstalled"];
            }
        }
        catch (Exception ex)
        {
            IsUpdateVisible = false;
            if (!silent)
            {
                StatusText = _localizationService.Format("Update_CheckFailed", ex.Message);
            }
        }
    }

    [RelayCommand]
    private async Task CheckForUpdate()
    {
        await CheckForUpdateAsync(silent: false);
    }

    [RelayCommand]
    private async Task DownloadUpdate()
    {
        if (_updateInfo == null || IsUpdating)
        {
            return;
        }

        IsUpdating = true;
        UpdateProgress = 0;
        UpdateBannerText = _localizationService["Update_Downloading"];
        UpdateButtonText = _localizationService["Update_DownloadingShort"];

        try
        {
            await _updateService.DownloadUpdatesAsync(_updateInfo, progress =>
            {
                Dispatcher.UIThread.Post(() => UpdateProgress = progress);
            });

            UpdateBannerText = _localizationService["Update_DownloadComplete"];
            await Task.Delay(500);
            _updateService.ApplyUpdatesAndRestart(_updateInfo);
        }
        catch (Exception ex)
        {
            IsUpdating = false;
            UpdateButtonText = _localizationService["Update_Retry"];
            UpdateBannerText = _localizationService.Format("Update_Failed", ex.Message);
        }
    }
}

using System;
using System.Threading.Tasks;
using NAudio.Wave;

namespace MsrPlayer.Services;

public class AudioService : IDisposable
{
    private IWavePlayer? _wavePlayer;
    private MediaFoundationReader? _reader;
    private bool _disposed;
    private bool _isStoppingManually;
    private System.Timers.Timer? _positionTimer;

    public event EventHandler? PlaybackEnded;
    public event EventHandler<PlaybackState>? StateChanged;
    public event EventHandler? PositionChanged;

    public PlaybackState State { get; private set; } = PlaybackState.Stopped;

    public TimeSpan CurrentTime
    {
        get { return _reader?.CurrentTime ?? TimeSpan.Zero; }
    }

    public TimeSpan TotalTime
    {
        get { return _reader?.TotalTime ?? TimeSpan.Zero; }
    }

    public float Volume
    {
        get { return _wavePlayer?.Volume ?? 1f; }
        set
        {
            if (_wavePlayer != null)
            {
                _wavePlayer.Volume = Math.Clamp(value, 0f, 1f);
            }
        }
    }

    public AudioService()
    {
        _positionTimer = new System.Timers.Timer(1000);
        _positionTimer.Elapsed += (_, _) => PositionChanged?.Invoke(this, EventArgs.Empty);
    }

    public async Task PlayAsync(string url)
    {
        _isStoppingManually = true;
        StopInternal();
        _isStoppingManually = false;

        try
        {
            _reader = new MediaFoundationReader(url);
            _wavePlayer = new WaveOutEvent();
            _wavePlayer.Init(_reader);
            _wavePlayer.Volume = Volume;
            _wavePlayer.PlaybackStopped += OnPlaybackStopped;

            _wavePlayer.Play();
            State = PlaybackState.Playing;
            StateChanged?.Invoke(this, State);
            _positionTimer?.Start();
            PositionChanged?.Invoke(this, EventArgs.Empty);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"播放失败: {ex.Message}");
            State = PlaybackState.Stopped;
            StateChanged?.Invoke(this, State);
        }
    }

    private void OnPlaybackStopped(object? sender, StoppedEventArgs e)
    {
        if (!_isStoppingManually)
        {
            _positionTimer?.Stop();
            State = PlaybackState.Stopped;
            StateChanged?.Invoke(this, State);
            PlaybackEnded?.Invoke(this, EventArgs.Empty);
        }
    }

    public void Pause()
    {
        try
        {
            if (_wavePlayer != null && _wavePlayer.PlaybackState == global::NAudio.Wave.PlaybackState.Playing)
            {
                _wavePlayer.Pause();
                State = PlaybackState.Paused;
                StateChanged?.Invoke(this, State);
                _positionTimer?.Stop();
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"暂停失败: {ex.Message}");
        }
    }

    public void Resume()
    {
        try
        {
            if (_wavePlayer != null && _wavePlayer.PlaybackState == global::NAudio.Wave.PlaybackState.Paused)
            {
                _wavePlayer.Play();
                State = PlaybackState.Playing;
                StateChanged?.Invoke(this, State);
                _positionTimer?.Start();
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"恢复播放失败: {ex.Message}");
        }
    }

    public void Stop()
    {
        _isStoppingManually = true;
        _positionTimer?.Stop();
        StopInternal();
        _isStoppingManually = false;
        State = PlaybackState.Stopped;
        StateChanged?.Invoke(this, State);
    }

    private void StopInternal()
    {
        try
        {
            if (_wavePlayer != null)
            {
                _wavePlayer.PlaybackStopped -= OnPlaybackStopped;
                _wavePlayer.Stop();
                _wavePlayer.Dispose();
            }

            if (_reader != null)
            {
                _reader.Dispose();
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"停止失败: {ex.Message}");
        }
        finally
        {
            _wavePlayer = null;
            _reader = null;
        }
    }

    public void Seek(TimeSpan position)
    {
        if (_reader != null)
        {
            _reader.CurrentTime = position;
            PositionChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            _isStoppingManually = true;
            _positionTimer?.Stop();
            _positionTimer?.Dispose();
            StopInternal();
            _disposed = true;
        }
    }
}

public enum PlaybackState
{
    Stopped,
    Playing,
    Paused
}
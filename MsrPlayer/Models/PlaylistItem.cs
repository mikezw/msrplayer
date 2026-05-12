using CommunityToolkit.Mvvm.ComponentModel;

namespace MsrPlayer.Models;

public partial class PlaylistItem : ObservableObject
{
    [ObservableProperty]
    private string _cid = string.Empty;

    [ObservableProperty]
    private string _name = string.Empty;

    [ObservableProperty]
    private string _artist = string.Empty;

    [ObservableProperty]
    private bool _isPlaying;

    [ObservableProperty]
    private bool _isCached;
}
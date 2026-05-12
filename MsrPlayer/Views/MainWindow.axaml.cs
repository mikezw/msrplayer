using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Platform.Storage;
using Avalonia.VisualTree;
using MsrPlayer.Models;
using MsrPlayer.ViewModels;

namespace MsrPlayer.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
    }

    private void OnSongDoubleTapped(object? sender, TappedEventArgs e)
    {
        if (sender is Border border && border.DataContext is Song song)
        {
            if (DataContext is MainWindowViewModel vm)
            {
                vm.AddToPlaylistCommand.Execute(song);
            }
        }
    }

    private void OnAddButtonTapped(object? sender, TappedEventArgs e)
    {
        if (sender is Border addBtn)
        {
            var parentBorder = addBtn.FindAncestorOfType<Border>();
            if (parentBorder?.DataContext is Song song)
            {
                if (DataContext is MainWindowViewModel vm)
                {
                    vm.AddToPlaylistCommand.Execute(song);
                }
            }
        }
    }

    private void OnPlaylistItemDoubleTapped(object? sender, TappedEventArgs e)
    {
        if (sender is Border border && border.DataContext is PlaylistItem item)
        {
            if (DataContext is MainWindowViewModel vm)
            {
                vm.PlayItemCommand.Execute(item);
            }
        }
    }

    private void OnRemoveButtonTapped(object? sender, TappedEventArgs e)
    {
        if (sender is Border removeBtn)
        {
            var parentBorder = removeBtn.FindAncestorOfType<Border>();
            if (parentBorder?.DataContext is PlaylistItem item)
            {
                if (DataContext is MainWindowViewModel vm)
                {
                    vm.RemoveItemCommand.Execute(item);
                }
            }
        }
    }

    private void OnUpdateCacheButtonTapped(object? sender, TappedEventArgs e)
    {
        if (sender is Border updateBtn)
        {
            var parentBorder = updateBtn.FindAncestorOfType<Border>();
            if (parentBorder?.DataContext is PlaylistItem item)
            {
                if (DataContext is MainWindowViewModel vm)
                {
                    vm.UpdateCacheCommand.Execute(item);
                }
            }
        }
    }

    private async void OnCacheSettingsTapped(object? sender, TappedEventArgs e)
    {
        if (DataContext is MainWindowViewModel vm)
        {
            var storageProvider = StorageProvider;
            var folders = await storageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
            {
                Title = "选择缓存目录",
                AllowMultiple = false
            });

            if (folders.Count > 0)
            {
                var folderPath = folders[0].Path.LocalPath;
                vm.SetCacheDirectory(folderPath);
            }
        }
    }
}
using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Media.Imaging;
using Avalonia.Markup.Xaml;
using Avalonia.Platform;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using MsrPlayer.Services;
using MsrPlayer.ViewModels;
using MsrPlayer.Views;

namespace MsrPlayer;

public partial class App : Application
{
    private MainWindow? _mainWindow;
    private TrayIcon? _trayIcon;
    private WindowIcon? _appIcon;
    private IServiceProvider? _services;

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
        LoadAppIcon();
        ConfigureServices();
    }

    private void ConfigureServices()
    {
        var services = new ServiceCollection();
        services.AddSingleton<ApiService>();
        services.AddSingleton<AudioService>();
        services.AddSingleton<PlaylistService>();
        services.AddSingleton<ConfigService>();
        services.AddSingleton<LyricService>();
        services.AddSingleton<CacheService>();
        services.AddSingleton<MainWindowViewModel>();
        _services = services.BuildServiceProvider();
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            _mainWindow = new MainWindow
            {
                DataContext = _services!.GetRequiredService<MainWindowViewModel>()
            };

            if (_appIcon != null)
            {
                _mainWindow.Icon = _appIcon;
            }

            _mainWindow.Closing += OnMainWindowClosing;
            desktop.MainWindow = _mainWindow;

            CreateTrayIcon();
        }

        base.OnFrameworkInitializationCompleted();
    }

    private void LoadAppIcon()
    {
        try
        {
            using var iconStream = AssetLoader.Open(new Uri("avares://MsrPlayer/Assets/music-icon.ico"));
            _appIcon = new WindowIcon(iconStream);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"图标加载失败: {ex.Message}");
        }
    }

    private void CreateTrayIcon()
    {
        try
        {
            if (_appIcon != null)
            {
                var menu = new NativeMenu();
                var showItem = new NativeMenuItem { Header = "显示窗口" };
                showItem.Click += (_, _) => ShowWindow();
                menu.Items.Add(showItem);

                var exitItem = new NativeMenuItem { Header = "退出" };
                exitItem.Click += (_, _) => ExitApp();
                menu.Items.Add(exitItem);

                _trayIcon = new TrayIcon
                {
                    Icon = _appIcon,
                    ToolTipText = "Monster Siren Player",
                    Menu = menu
                };

                _trayIcon.Clicked += (_, _) => ShowWindow();
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"托盘图标创建失败: {ex.Message}");
        }
    }

    private void OnMainWindowClosing(object? sender, WindowClosingEventArgs e)
    {
        e.Cancel = true;
        _mainWindow?.Hide();
    }

    [RelayCommand]
    private void ShowWindow()
    {
        if (_mainWindow != null)
        {
            _mainWindow.Show();
            _mainWindow.Activate();
        }
    }

    [RelayCommand]
    private void ExitApp()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            if (_mainWindow != null)
            {
                _mainWindow.Closing -= OnMainWindowClosing;
            }

            _trayIcon?.Dispose();
            desktop.Shutdown();
        }
    }
}
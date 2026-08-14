using System;
using System.Threading;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Media.Imaging;
using Avalonia.Markup.Xaml;
using Avalonia.Platform;
using Avalonia.Threading;
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
    private CancellationTokenSource? _singleInstanceCts;
    private NativeMenuItem? _showItem;
    private NativeMenuItem? _updateItem;
    private NativeMenuItem? _exitItem;

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
        services.AddSingleton<UpdateService>();
        services.AddSingleton<ILocalizationService, LocalizationService>();
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
            StartSingleInstanceListener();
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
            System.Diagnostics.Debug.WriteLine($"Icon load failed: {ex.Message}");
        }
    }

    private void CreateTrayIcon()
    {
        try
        {
            if (_appIcon != null)
            {
                var menu = new NativeMenu();
                _showItem = new NativeMenuItem { Header = "Show Window" };
                _showItem.Click += (_, _) => ShowWindow();
                menu.Items.Add(_showItem);

                _updateItem = new NativeMenuItem { Header = "Check for Updates" };
                _updateItem.Click += (_, _) => _services!.GetRequiredService<MainWindowViewModel>().CheckForUpdateCommand.Execute(null);
                menu.Items.Add(_updateItem);

                _exitItem = new NativeMenuItem { Header = "Exit" };
                _exitItem.Click += (_, _) => ExitApp();
                menu.Items.Add(_exitItem);

                _trayIcon = new TrayIcon
                {
                    Icon = _appIcon,
                    ToolTipText = "Monster Siren Player",
                    Menu = menu
                };

                _trayIcon.Clicked += (_, _) => ShowWindow();

                var localizationService = _services!.GetRequiredService<ILocalizationService>();
                localizationService.LanguageChanged += (_, _) => UpdateTrayMenuTexts();
                UpdateTrayMenuTexts();
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Tray icon creation failed: {ex.Message}");
        }
    }

    private void UpdateTrayMenuTexts()
    {
        var loc = _services?.GetService<ILocalizationService>();
        if (loc == null)
        {
            return;
        }

        if (_showItem != null)
        {
            _showItem.Header = loc["Common_ShowWindow"];
        }

        if (_updateItem != null)
        {
            _updateItem.Header = loc["Common_CheckForUpdates"];
        }

        if (_exitItem != null)
        {
            _exitItem.Header = loc["Common_Exit"];
        }
    }

    /// <summary>
    /// Listens for activation requests from a second instance and shows
    /// the window on the UI thread when one arrives.
    /// </summary>
    private void StartSingleInstanceListener()
    {
        _singleInstanceCts = new CancellationTokenSource();
        _ = SingleInstanceManager.StartListenerAsync(ShowWindowFromListener, _singleInstanceCts.Token);
    }

    /// <summary>
    /// Shows the window on the UI thread and returns its native handle so
    /// the second instance can bring it to the foreground.
    /// </summary>
    private string? ShowWindowFromListener()
    {
        string? handle = null;
        Dispatcher.UIThread.InvokeAsync(() =>
        {
            ShowWindow();
            handle = _mainWindow?.TryGetPlatformHandle() is { } platformHandle && platformHandle.Handle != IntPtr.Zero
                ? platformHandle.Handle.ToString()
                : null;
        }).GetAwaiter().GetResult();
        return handle;
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
            if (_mainWindow.WindowState == WindowState.Minimized)
            {
                _mainWindow.WindowState = WindowState.Normal;
            }

            _mainWindow.Show();
            _mainWindow.Activate();
        }
    }

    [RelayCommand]
    private void ExitApp()
    {
        _singleInstanceCts?.Cancel();
        _singleInstanceCts?.Dispose();
        _singleInstanceCts = null;

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
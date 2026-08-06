using Avalonia;
using System;
using Velopack;

namespace MsrPlayer;

sealed class Program
{
    [STAThread]
    public static void Main(string[] args)
    {
        // Velopack 必须在 Main 最前面初始化，用于处理安装/更新/卸载钩子
        VelopackApp.Build().Run();

        // Single instance guard: a second launch notifies the running
        // instance to show its window, then exits without starting a new one.
        if (!SingleInstanceManager.TryAcquireMutex(out var mutex))
        {
            SingleInstanceManager.ActivateExistingInstanceAsync().GetAwaiter().GetResult();
            return;
        }

        using (mutex)
        {
            BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
        }
    }

    public static AppBuilder BuildAvaloniaApp()
    {
        return AppBuilder.Configure<App>()
            .UsePlatformDetect()
#if DEBUG
            .WithDeveloperTools()
#endif
            .WithInterFont()
            .LogToTrace();
    }
}
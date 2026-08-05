using System;
using System.Threading.Tasks;
using Velopack;
using Velopack.Sources;

namespace MsrPlayer.Services;

public class UpdateService
{
    private const string GithubRepoUrl = "https://github.com/mikezw/msrplayer";

    private readonly UpdateManager _updateManager;

    public UpdateService()
    {
        _updateManager = new UpdateManager(new GithubSource(GithubRepoUrl, null, false));
    }

    /// <summary>
    /// 检查是否有新版本。
    /// 非安装版（如直接从 bin 运行，无 Velopack 安装记录）会抛出 NotInstalledException，由调用方决定提示；
    /// 未发布版本时返回 null。
    /// </summary>
    public async Task<UpdateInfo?> CheckForUpdatesAsync()
    {
        return await _updateManager.CheckForUpdatesAsync();
    }

    public async Task DownloadUpdatesAsync(UpdateInfo updateInfo, Action<int>? onProgress = null)
    {
        if (onProgress == null)
        {
            await _updateManager.DownloadUpdatesAsync(updateInfo);
        }
        else
        {
            await _updateManager.DownloadUpdatesAsync(updateInfo, p => onProgress(p));
        }
    }

    public void ApplyUpdatesAndRestart(UpdateInfo updateInfo)
    {
        _updateManager.ApplyUpdatesAndRestart(updateInfo.TargetFullRelease);
    }

    public static bool IsNewerVersion(string? currentVersion, string? latestVersion)
    {
        if (!Version.TryParse(currentVersion, out var current) ||
            !Version.TryParse(latestVersion, out var latest))
        {
            return false;
        }

        return latest > current;
    }
}

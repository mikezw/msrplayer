# Monster Siren Player

一个基于 Avalonia UI 的塞壬唱片（https://monster-siren.hypergryph.com/）音乐播放器。

> 本项目完全由 AI 编写。

## 功能

- 歌曲列表展示与搜索
- 播放列表管理
- 播放控制（播放/暂停/上一首/下一首）
- 播放模式切换（顺序/单曲循环/列表循环）
- 实时歌词显示
- 音频缓存系统（边下边播）
- 缓存目录配置
- 系统托盘支持

## 技术栈

- .NET 10
- Avalonia UI 12
- CommunityToolkit.Mvvm
- NAudio (音频播放)

## 运行

```bash
cd MsrPlayer
dotnet run
```

## 构建

```bash
dotnet build
```

## 配置

配置文件位于 `%APPDATA%\MsrPlayer\config.json`：
- 音量设置
- 播放模式
- 缓存目录
- 是否启用缓存

缓存目录默认为程序目录下的 `cache` 文件夹。
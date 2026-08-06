# MSR Player - AI Agent Guide

> Monster Siren Records 音乐播放器 - AI 协作开发指南

## 项目概述

MSR Player 是一个基于 Avalonia UI 的跨平台桌面音乐播放器，用于播放塞壬唱片（Monster Siren Records）的音乐。项目完全由 AI 编写，采用 MVVM 架构设计。

### 核心功能
- 歌曲列表展示与实时搜索
- 播放列表管理
- 播放控制（播放/暂停/上一首/下一首）
- 三种播放模式（顺序/单曲循环/列表循环）
- 实时歌词显示（LRC格式）
- 音频缓存系统（边下边播）
- 系统托盘支持
- 单实例运行（重复启动时唤醒已有实例）
- 自动更新（Velopack）

## 技术栈

### 核心框架
- **运行时**: .NET 10
- **UI框架**: Avalonia UI 12
- **MVVM框架**: CommunityToolkit.Mvvm 8.4.2
- **音频引擎**: NAudio 2.3.0

### 依赖注入
- Microsoft.Extensions.DependencyInjection 10.0.7

## 架构设计

### 项目结构
```
MsrPlayer/
├── Models/              # 数据模型层
│   ├── Song.cs          # 歌曲基础信息
│   ├── SongDetail.cs    # 歌曲详情（含播放地址）
│   ├── LyricLine.cs     # 歌词行模型
│   └── PlaylistItem.cs  # 播放列表项
│
├── Services/            # 服务层（业务逻辑）
│   ├── ApiService.cs       # Monster Siren API 调用
│   ├── AudioService.cs     # 音频播放控制
│   ├── CacheService.cs     # 缓存管理
│   ├── ConfigService.cs    # 配置持久化
│   ├── LyricService.cs     # 歌词解析
│   ├── PlaylistService.cs  # 播放列表持久化
│   └── UpdateService.cs    # Velopack 自动更新
│
├── ViewModels/          # MVVM ViewModel层
│   ├── ViewModelBase.cs             # MVVM基类
│   └── MainWindowViewModel.cs       # 主窗口ViewModel
│
├── Views/               # Avalonia UI视图层
│   ├── MainWindow.axaml            # 主窗口XAML
│   └── MainWindow.axaml.cs         # 主窗口代码后台
│
├── Program.cs                # 程序入口（含单实例保护）
├── SingleInstanceManager.cs  # 单实例检测与唤醒（Mutex + NamedPipe）
└── App.axaml.cs              # 应用程序入口与依赖注入配置
```

### 设计模式

#### MVVM 架构
- **Model**: 纯数据模型，无业务逻辑
- **View**: Avalonia XAML 声明式UI，使用数据绑定
- **ViewModel**: 业务逻辑与UI状态管理，使用 `[ObservableProperty]` 和 `[RelayCommand]`

#### 依赖注入 (DI)
所有服务在 `App.axaml.cs` 中注册为单例：
```csharp
services.AddSingleton<ApiService>();
services.AddSingleton<AudioService>();
services.AddSingleton<PlaylistService>();
services.AddSingleton<ConfigService>();
services.AddSingleton<LyricService>();
services.AddSingleton<CacheService>();
services.AddSingleton<UpdateService>();
services.AddSingleton<MainWindowViewModel>();
```

#### 事件驱动
服务通过事件通知状态变化：
- `AudioService.PlaybackEnded` - 播放结束
- `AudioService.StateChanged` - 播放状态变化
- `AudioService.PositionChanged` - 播放位置更新

## 关键组件

### 1. ApiService - API通信
**职责**: 与 Monster Siren API 交互
- Base URL: `https://monster-siren.hypergryph.com/api`
- 无需鉴权（不需要 Token、Cookie、签名）
- 主要端点：
  - `GET /songs` - 获取全部歌曲列表
  - `GET /song/{cid}` - 获取单曲详情（播放地址、歌词）
  - `GET /albums` - 获取专辑列表

**音频格式**: WAV无损格式，HTTP直链

### 2. AudioService - 音频播放
**职责**: 音频播放控制与状态管理
- 使用 `NAudio.Wave.MediaFoundationReader` 读取音频流
- 支持边下边播（直接播放HTTP音频流）
- 提供 Play/Pause/Resume/Stop/Seek 操作
- 通过事件通知播放进度和状态变化

**关键状态**:
- `PlaybackState`: Stopped/Playing/Paused
- `CurrentTime`/`TotalTime`: 播放时间信息
- `Volume`: 音量控制 (0.0-1.0)

### 3. CacheService - 缓存管理
**职责**: 本地缓存音频、歌词、歌曲详情
- 缓存目录结构:
  ```
  cache/
  ├── audio/{cid}_{hash}.wav    # 音频文件
  ├── lyrics/{cid}.lrc          # 歌词文件
  └── songs/{cid}.json          # 歌曲详情
  ```
- 提供缓存查询、保存、删除功能
- 支持边下边播：先播放在线流，后台下载缓存

### 4. MainWindowViewModel - 主逻辑
**职责**: UI状态管理与业务协调

**核心方法**:
- `LoadDataAsync()`: 启动时加载歌曲列表和播放列表
- `PlaySongAtIndex(int index)`: 播放指定索引的歌曲
- `FilterSongs()`: 实时搜索过滤
- `OnPlaybackEnded()`: 播放结束自动切换下一首

**状态属性**:
- `Songs`: 全部歌曲列表
- `Playlist`: 播放列表
- `CurrentIndex`: 当前播放索引
- `CurrentMode`: 播放模式
- `SearchText`: 搜索文本

### 5. SingleInstanceManager - 单实例管理
**职责**: 保证程序只有一个实例运行，重复启动时唤醒已有实例（不启动新进程）
- 命名 Mutex 检测实例唯一性（在 `Program.Main` 中获取，获取失败则通知已有实例后退出）
- 命名管道 IPC 握手：第二实例发送 `ShowWindow` 命令，第一实例在 UI 线程显示窗口并把窗口句柄回传
- 由第二实例（用户刚启动、拥有前台权限）调用 `SetForegroundWindow` 置前窗口，规避 Windows 前台锁限制（第一实例自身调用会被系统拒绝）
- 主程序代码不含 Windows API，唯一的 user32 调用位于第二实例的退出路径（仅 Windows 生效，其他平台由第一实例的 `Activate()` 兜底）

## UI设计规范

### 主题色
- 主色调: `#1DB954` (Spotify绿)
- 背景色: `#121212` (深灰)
- 次级背景: `#1e1e1e`
- 文字色: `#e0e0e0` / `#a0a0a0`

### 列表项样式
```xml
<!-- 默认状态 -->
Background="Transparent"
BorderBrush="Transparent"

<!-- 悬停状态 -->
Background="#252525"
BorderBrush="#1DB954"
```

### 布局结构
```
[歌曲列表] | [播放控制] | [播放列表]
   4/10    |    3/10    |    3/10
```

## 配置管理

### 配置文件位置
`%APPDATA%\MsrPlayer\config.json`

### 配置项
```json
{
  "Volume": 75.0,
  "PlayMode": 0,
  "CacheDirectory": "...",
  "EnableCache": true
}
```

### 播放模式枚举
- `Sequence`: 顺序播放
- `LoopOne`: 单曲循环
- `LoopAll`: 列表循环

## 开发流程

### 构建和运行
```bash
cd MsrPlayer
dotnet restore
dotnet build
dotnet run
```

### 发布
```bash
dotnet publish -c Release -r win-x64 --self-contained
```

### CI/CD
- GitHub Actions: `.github/workflows/dotnet.yml`
- 触发方式: 自动（推送 `v*` 格式 tag）+ 手动 `workflow_dispatch`
- 步骤: restore → test → publish → pack → release（publish 及后续步骤仅 tag 触发时执行）

## 代码规范

### 命名约定
- **类名**: PascalCase (如 `AudioService`)
- **方法名**: PascalCase (如 `PlaySongAtIndex`)
- **私有字段**: `_camelCase` (如 `_audioService`)
- **属性**: PascalCase (如 `CurrentIndex`)

### 提交与注释
- **提交记录**（git commit message）使用英文
- **代码注释**（含 CI 配置、脚本注释）使用英文

### MVVM 属性定义
```csharp
[ObservableProperty]
private string _propertyName;
```

### 命令定义
```csharp
[RelayCommand]
private void MethodName() { }
```

### 属性变化监听
```csharp
partial void OnPropertyNameChanged(string value)
{
    // 响应属性变化
}
```

### 异步方法命名
- 以 `Async` 结尾: `LoadDataAsync()`
- 事件处理使用 `async void`: `private async void OnPlaybackEnded()`

## API 参考

详细 API 文档请参考: [docs/api.md](docs/api.md)

### 基础信息
- Base URL: `https://monster-siren.hypergryph.com/api`
- 无需鉴权
- 音频格式: WAV无损
- 响应格式: JSON

### 主要端点
| 端点 | 说明 | 返回 |
|------|------|------|
| GET /songs | 获取全部歌曲 | `list: Song[]` |
| GET /song/{cid} | 获取歌曲详情 | `SongDetail` |
| GET /albums | 获取专辑列表 | `list: Album[]` |
| GET /album/{albumCid} | 获取专辑详情 | `Album` |

## 注意事项

### 音频播放
- 使用 HTTP 直链播放，无需鉴权
- 支持 WAV 无损格式
- 边下边播时，先播放网络流，后台下载缓存

### 歌词解析
- LRC 格式: `[mm:ss.xx]歌词文本`
- 解析后存储为 `LyricLine` 对象列表

### 缓存机制
- 音频缓存: 根据 URL hash 判断是否需要更新
- 歌词缓存: 直接缓存 lrc 文件内容
- 歌曲详情: 缓存完整 JSON 对象

### 跨平台兼容
- Avalonia UI 支持跨平台
- NAudio 主要用于 Windows，其他平台可能需要适配
- 配置路径使用 `Environment.SpecialFolder.ApplicationData`

## 已知限制

1. **平台限制**: NAudio 主要支持 Windows，Linux/macOS 需要替换音频库

## 扩展建议

### 优先级高
1. 实现歌词滚动效果（当前位置高亮）
2. 添加专辑封面显示
3. 支持播放历史记录

### 优先级低
1. Linux/macOS 音频库适配（如使用 OpenAL）
2. 桌面通知支持
3. 多语言支持

---

*此文档由 AI 生成，用于帮助 AI Agent 和开发者理解项目架构和开发规范。*
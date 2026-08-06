# 发布与自动更新文档

基于 Velopack 1.2.0 的多架构发布流程，时间：2026-08

## 1. 概述

本项目使用 **Velopack** 作为自动更新框架，发布时由 GitHub Actions 自动完成打包与上传。

- 支持平台：Windows x64、Windows arm64（各架构独立更新源）
- 自动更新：客户端通过 `UpdateService` 检查并下载更新
- 发布触发：推送 `v*` 格式的 git tag

## 2. 多架构发布方案（channel 区分）

Velopack 通过 **独立 channel** 区分不同架构，产物命名自动携带 channel 标识，互不冲突：

| 项 | win-x64 | win-arm64 |
|----|---------|-----------|
| vpk channel | `win`（默认） | `win-arm64` |
| 安装包 | `MsrPlayer-win-x64-{版本}-Setup.exe` | `MsrPlayer-win-arm64-{版本}-Setup.exe` |
| MSI 安装包 | `MsrPlayer-win-x64-{版本}.msi` | `MsrPlayer-win-arm64-{版本}.msi` |
| 更新包 | `MsrPlayer-{版本}-full.nupkg` | `MsrPlayer-{版本}-win-arm64-full.nupkg` |
| 便携版 | `MsrPlayer-win-Portable.zip` | `MsrPlayer-win-arm64-Portable.zip` |
| 更新源 | `releases.win.json` | `releases.win-arm64.json` |
| 增量清单 | `RELEASES` | `RELEASES-win-arm64` |

**安装方式**：`Setup.exe` 为一键静默安装（固定安装到 `%LocalAppData%\MsrPlayer`，无路径选择，Velopack 设计如此）；`.msi`（`--instLocation Either`）安装时可由用户选择安装作用域（PerUser → `%LocalAppData%` / PerMachine → `Program Files`），更新机制与 Setup.exe 完全一致。

**更新机制自洽**：安装器会记录安装时的 channel，客户端检查更新时自动读取对应架构的更新源（arm64 机器读 `releases.win-arm64.json`，x64 机器读 `releases.win.json`），两个架构互不干扰。

## 3. CI 发布流程（.github/workflows/dotnet.yml）

触发条件：

- `push` + tag（自动）：推送 `v*` 格式 tag 时自动触发，如 `v1.0.0`，版本号取自 `${GITHUB_REF_NAME#v}`。**每个 tag 推送触发一次独立运行**，删除远程 tag 不触发
- `workflow_dispatch`（手动）：手动触发时可选择分支或 tag；选 tag 时 `github.ref_type == 'tag'`，发布步骤才会执行（选分支则只跑构建和测试）

tag 推送后的步骤：

1. **构建 + 测试**：`dotnet restore`（无 RID，提供基础 assets）→ `dotnet test --no-restore`（test 步骤隐含构建）
2. **publish and pack**（bash 循环两个架构 `"win-x64:win" "win-arm64:win-arm64"`）：
   - `dotnet publish -r:{RID} -p:Version={版本}`：**不带 `--no-restore`**，SDK 隐式 restore 自动补齐当前 RID 的 assets（不能手动连续 `dotnet restore -r`，后一次会覆盖前一次的 RID target）
   - `vpk download github -c {CHANNEL} || true`：拉取上一版本产物用于生成增量更新包（首次发布无上一版本，忽略失败）
   - `vpk '[win]' pack --runtime {RID} --channel {CHANNEL} --msi --instLocation Either`：生成安装包、MSI 安装包（安装时可选 PerUser/PerMachine）、全量更新包、便携版、更新源。runner 为 Linux，打包 Windows 包必须加 OS 指令 `[win]`（vpk 的 System.CommandLine directive，**方括号是语法的一部分**；无指令时目标 OS 默认当前系统，`vpk pack` 会报 "target rid must be Linux"）
   - 安装包重命名：`MsrPlayer-{CHANNEL}-Setup.exe` → `MsrPlayer-{RID}-{版本}-Setup.exe`、`MsrPlayer-{CHANNEL}.msi` → `MsrPlayer-{RID}-{版本}.msi`（带架构 + 版本后缀，便于用户识别；不影响自动更新，更新源只引用 nupkg）
   - 仅对安装文件（Setup.exe / MSI）生成 SHA256 校验文件（`*.sha256`），其他产物不生成
3. **release**：`gh release create {tag} --generate-notes` + `gh release upload Releases/* --clobber`，全部产物上传到对应 tag 的 GitHub Release

> 说明：不使用 `vpk upload`，因为它只识别 Velopack 标准资产，重命名后的安装包和 `.sha256` 文件可能被遗漏；`gh release upload` 可上传任意文件。使用 `--clobber` 覆盖同 tag 重跑的上传。

## 4. Release 资产清单（以 v1.0.0 为例）

| 资产 | 说明 |
|------|------|
| `MsrPlayer-win-x64-1.0.0-Setup.exe` | x64 安装包（一键安装，无需管理员） |
| `MsrPlayer-win-x64-1.0.0-Setup.exe.sha256` | x64 安装包校验值 |
| `MsrPlayer-win-x64-1.0.0.msi` | x64 MSI 安装包（安装时可选安装作用域） |
| `MsrPlayer-win-x64-1.0.0.msi.sha256` | x64 MSI 安装包校验值 |
| `MsrPlayer-win-arm64-1.0.0-Setup.exe` | arm64 安装包（一键安装，无需管理员） |
| `MsrPlayer-win-arm64-1.0.0-Setup.exe.sha256` | arm64 安装包校验值 |
| `MsrPlayer-win-arm64-1.0.0.msi` | arm64 MSI 安装包（安装时可选安装作用域） |
| `MsrPlayer-win-arm64-1.0.0.msi.sha256` | arm64 MSI 安装包校验值 |
| `MsrPlayer-1.0.0-full.nupkg` | x64 全量更新包 |
| `MsrPlayer-1.0.0-win-arm64-full.nupkg` | arm64 全量更新包 |
| `MsrPlayer-win-Portable.zip` | x64 便携版 |
| `MsrPlayer-win-arm64-Portable.zip` | arm64 便携版 |
| `releases.win.json` / `RELEASES` | x64 更新源 |
| `releases.win-arm64.json` / `RELEASES-win-arm64` | arm64 更新源 |

## 5. 版本与 tag 规范

| 项 | 规则 |
|----|------|
| 项目版本 | `MsrPlayer.csproj` 的 `<Version>` 固定为 `0.0.0`，仅本地开发构建使用（占位） |
| 发布 tag | `v{版本}`，如 `v1.0.0`，**发布版本以 tag 为准**；CI 通过 `-p:Version={版本}` 和 `vpk --packVersion` 统一使用 tag 版本，与 csproj 无关 |
| 增量更新 | 需保留历史版本的 nupkg（Release 资产中有），Velopack 据此生成 delta 包 |
| 首次发布 | `vpk download` 失败属正常，忽略后直接打全量包 |

## 6. 客户端自动更新

- `UpdateService` 基于 `GithubSource` 指向仓库 `mikezw/msrplayer` 的 Release 资产
- 启动时静默检查，发现新版本显示更新横幅，可手动触发（托盘菜单"检查更新"）
- 下载更新后调用 `ApplyUpdatesAndRestart` 重启应用完成安装
- 非安装环境（如直接运行 bin/发布目录）检查更新会提示"当前为非安装版本，无法检查更新"（静默启动检查时忽略）

## 7. 代码签名（预留）

当前未启用签名，未签名版本在 Windows 上会显示 SmartScreen 警告，属正常现象。

启用方式（任选其一，在 CI 的 `vpk pack` 命令追加参数）：

| 方案 | 参数 |
|------|------|
| Azure Trusted Signing | `--azureTrustedSignFile <metadata.json>` |
| 已有证书 | `--signTemplate "signtool sign /f <cert.pfx> /p <密码> {{file}}"` |

证书文件与密码通过 GitHub Secrets 注入，禁止明文写入仓库。

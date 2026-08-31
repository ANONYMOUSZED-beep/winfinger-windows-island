# WinFinger

一个 Windows 灵动岛效率工具 —— macOS 刘海工具 [MacFinger] 的 Windows 移植 + 灵动岛增强版。

屏幕顶部常驻一个 iOS 灵动岛风格的黑色胶囊，实时显示网速与内存；点击弹性展开为五页面板：**剪贴板历史 / 媒体控制 / 便利贴 / 快捷键 / 番茄钟**。

## 功能

| 模块 | 说明 |
|---|---|
| 紧凑岛 | 顶部居中黑色胶囊：左侧媒体封面（播放时），右侧 `↓下行 ↑上行 · 内存%` 每秒刷新 |
| 剪贴板历史 | 事件驱动监听（WM_CLIPBOARDUPDATE），文本+图片，SHA256 去重，上限 100 条，图片落盘 PNG，可暂停/清空/一键回贴，记录来源应用 |
| 媒体控制 | 系统全局媒体会话（GSMTC）：封面/标题/艺术家，播放暂停/上下曲，支持 Spotify、浏览器、网易云等 |
| 便利贴 | 列表+编辑器，自动保存（500ms 去抖），置顶排序，Ctrl+N 新建 |
| 快捷键词典 | 按前台应用自动切换（资源管理器/Chrome/Edge/VS Code/Word/Excel/微信/Terminal），无匹配显示 Windows 通用快捷键 |
| 番茄钟 | 专注/休息循环（时长可调），紧凑岛显示倒计时，到点岛内弹通知+提示音 |
| 岛内通知 | 复制捕获、番茄到点等事件触发胶囊“鼓起”通知条 3 秒 |
| 音频可视化 | 播放音乐时胶囊内 8 根频谱条实时跳动（WASAPI loopback + FFT） |
| 封面取色辉光 | 从专辑封面提取主色，岛体外圈随播放呼吸脉动的彩色辉光 |
| 悬停预展开 | 鼠标悬停胶囊轻微放大并露出歌名，点击才全展开 |

## 交互

- **点击胶囊** 展开 / **Esc** 或 **点击面板外** 收起
- **Ctrl+1..5** 切换页面（剪贴板/媒体/便签/快捷键/番茄钟）
- 托盘图标：打开、暂停剪贴板、清空历史、开机自启、退出
- 无任务栏图标、不出现在 Alt-Tab

## 技术栈

WPF / .NET 8（`net8.0-windows10.0.19041.0`，内置 CsWinRT 投影调用 WinRT 媒体 API），MVVM（CommunityToolkit.Mvvm），托盘用 Hardcodet.NotifyIcon.Wpf。Per-Monitor V2 DPI。

窗口方案：透明无边框置顶窗口固定为最大尺寸，仅对内部 Border 的宽/高/圆角做 Storyboard 动画（展开 `BackEase` 弹性 280ms，收起 `CubicEase` 180ms），透明像素天然点击穿透。

> 已知裁剪：不接管系统 Toast 通知——WinRT `UserNotificationListener` 需要 MSIX package identity，unpackaged exe 不可用；岛内通知仅承载应用自有事件。

## 构建

需要 .NET 8 SDK：

```bash
dotnet build                # 调试
dotnet run --project src/WinFinger
```

发布单文件（约 75MB，含运行时，无需安装 .NET）：

```bash
dotnet publish src/WinFinger/WinFinger.csproj -c Release -r win-x64 --self-contained \
  -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true \
  -p:EnableCompressionInSingleFile=true
```

产物：`src/WinFinger/bin/Release/net8.0-windows10.0.19041.0/win-x64/publish/WinFinger.exe`

## 本地数据

```
%APPDATA%\WinFinger\
├── clipboard.json      # 剪贴板元数据（字段与 mac 版兼容）
├── notes.json          # 便利贴
├── settings.json       # 设置
└── ClipboardMedia\     # 剪贴板图片 PNG
```

## 环境要求

- Windows 10 1809+（Windows 11 最佳）
- 剪贴板可能包含敏感内容（密码等），历史以明文存本地磁盘，请知悉；可随时暂停记录或清空

## 许可证

仅供学习和个人使用。

# ScrcpyGUI

一个面向 Windows 的 [scrcpy](https://github.com/Genymobile/scrcpy) 图形化启动器和控制面板。它把常用启动参数、已启动窗口快捷操作和手机长截图整合到一个界面中，日常使用不会弹出 CMD 窗口。

> 本项目不是 scrcpy 官方项目，也不包含 scrcpy、ADB 或 FFmpeg 二进制文件。使用前请先下载官方 scrcpy Windows 版本。

## 功能

- 自动发现 USB / 无线 ADB 设备
- 图形化设置分辨率、码率、帧率、编码器和画面方向
- 全屏、置顶、关闭手机屏幕、保持唤醒、显示触摸点
- 音频控制、只读控制、录屏和自定义 scrcpy 参数
- 管理由本程序启动的多个 scrcpy 会话
- 对已启动窗口发送 scrcpy 快捷键：全屏、旋转、缩放、暂停、导航键、音量、电源、通知栏、剪贴板和 FPS 等
- 手机原生长截图：触发系统长截图并将结果复制到电脑剪贴板
- 电脑兼容拼接：自动截图、滚动、识别重叠区域，同时处理固定顶部和固定底栏
- 长截图默认最大拼接屏数为 99，结果只进入 Windows 剪贴板，不在电脑上生成图片文件

## 环境要求

- Windows 10 或 Windows 11
- 一台已开启 USB 调试或无线调试的 Android 设备
- 官方 [scrcpy Windows 发行版](https://github.com/Genymobile/scrcpy/releases)（本项目当前按 scrcpy 4.1 测试）
- 构建源码时需要 Windows 自带的 .NET Framework 4.x C# 编译器

## 安装

1. 从 scrcpy 官方 Releases 下载并解压 Windows 版本。
2. 下载本仓库中的 `ScrcpyGUI.exe`。
3. 把 `ScrcpyGUI.exe` 放到 `scrcpy.exe` 和 `adb.exe` 所在目录。
4. 双击 `ScrcpyGUI.exe`。

程序采用 Windows GUI 子系统构建，启动时不会显示 CMD 窗口。当前可执行文件未进行代码签名，Windows SmartScreen 首次运行时可能显示提示。

## 基本使用

### USB 连接

1. 在手机的开发者选项中开启 USB 调试。
2. 用数据线连接电脑，并在手机上允许本机的调试授权。
3. 打开 ScrcpyGUI，点击“刷新设备”。
4. 选择设备和需要的参数，点击“启动 Scrcpy”。

### 无线连接

1. 确保手机已开启无线调试，或已经通过 USB 将 ADB 切换到 TCP/IP 模式。
2. 在无线连接输入框填写 `IP:端口`，例如 `192.168.1.100:5555`。
3. 点击“连接”，随后选择设备并启动 scrcpy。

### 已启动窗口快捷操作

1. 先从 ScrcpyGUI 启动至少一个 scrcpy 窗口。
2. 点击“窗口快捷操作”。
3. 在下拉框中选择仍在运行的窗口，然后点击所需功能。

快捷按钮使用 scrcpy 默认的 `Left Alt` 修饰键。若要使用此面板，请不要在额外参数中修改 `--shortcut-mod`。

### 电脑兼容拼接长截图

该模式适合原生长截图不稳定，或者页面存在固定标题栏、固定底部按钮的场景。

1. 在手机上打开目标页面，并滚动到内容顶部。
2. 在 ScrcpyGUI 中点击“手机长截图”。
3. 选择“电脑兼容拼接”。
4. 保持页面稳定，等待程序自动截图、滚动和拼接。
5. 完成后直接在微信、Word、画图等软件中按 `Ctrl+V` 粘贴。

兼容拼接默认采用约 35% 的慢速滑动，并通过相邻画面的边缘模板匹配查找重叠区域。固定顶部和固定底栏只保留一份；最大拼接屏数默认是 99，检测到页面底部时会提前结束。

### 手机原生长截图

1. 点击“手机长截图”，选择“手机原生长截图”。
2. 自动模式会尝试触发系统截图浮层、点击“截长图”，并在到达底部后点击“保存”。
3. 手动模式下，你可以自己控制滚动范围并点击手机上的“保存”；程序会等待新图片并复制到 Windows 剪贴板。

不同手机厂商的截图浮层实现差异较大。如果按钮识别不稳定，请使用“电脑兼容拼接”。原生模式生成的图片会保留在手机相册；电脑端仍不额外保存文件。

## 从源码构建

把仓库放在官方 scrcpy 解压目录内，使目录结构类似：

```text
scrcpy-win64-v4.1/
├─ adb.exe
├─ scrcpy.exe
├─ scrcpy-server
├─ gui-src/
│  ├─ Program.cs
│  ├─ app.manifest
│  └─ build.ps1
└─ ScrcpyGUI.exe
```

然后在 PowerShell 中运行：

```powershell
powershell -ExecutionPolicy Bypass -File .\gui-src\build.ps1
```

构建脚本调用 Windows 自带的 .NET Framework C# 编译器，输出文件为仓库根目录下的 `ScrcpyGUI.exe`。

## 已知限制

- 动态视频、自动刷新的广告、瀑布流和持续变化的悬浮控件可能影响拼接结果。
- 99 屏是安全上限而不是推荐的固定长度；超长页面会占用更多时间和内存。
- 原生长截图依赖手机厂商的系统界面和按钮文本，无法保证适配所有 Android 系统。
- 窗口快捷操作仅管理由当前 ScrcpyGUI 进程启动的 scrcpy 会话。

## 隐私

ScrcpyGUI 不包含遥测、账户系统或云端上传功能。设备控制、截图和拼接都在本机通过 ADB 完成。请只连接你有权控制的设备。

## 致谢与第三方项目

- [Genymobile/scrcpy](https://github.com/Genymobile/scrcpy) — Android 屏幕镜像与控制，Apache-2.0
- [xutianyi1999/scrollshot](https://github.com/xutianyi1999/scrollshot) — 长截图拼接思路参考，MIT
- [jaflo/screenStitch](https://github.com/jaflo/screenStitch) — 屏幕拼接思路参考，MIT
- [ShareX Scrolling Screenshot](https://getsharex.com/docs/scrolling-screenshot.html) — 滚动截图流程参考

本项目没有把上述项目的二进制文件打包进仓库。详情见 [THIRD_PARTY_NOTICES.md](THIRD_PARTY_NOTICES.md)。

## 许可证

ScrcpyGUI 自有源码使用 [MIT License](LICENSE) 开源。scrcpy 及其他第三方组件分别遵循其自身许可证。

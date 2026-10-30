# 本地视频播放器（Local Video Player）

Windows 桌面本地视频播放器，面向正式产品与开源发布。技术路线：**.NET 8 + WPF + libmpv + SQLite**。

## 核心特性

- **播放内核**：libmpv（基于 FFmpeg），多后端硬件解码（D3D11VA / D3D12VA / DXVA2 + NVDEC / QSV），全格式硬解（H.264 / HEVC / VP9 / AV1，含 10bit 与 HDR），失败自动回退软解；gpu-next 渲染管线
- **音频**：AAC / MP3 / FLAC / APE / DTS / AC3 / TrueHD，WASAPI 独占输出、位深匹配与高质量重采样
- **字幕**：自动关联同目录字幕（多语言候选、编码检测防乱码），内嵌字幕轨道选择与样式定制
- **媒体库**：目录递归扫描、SQLite 索引、封面墙/列表、搜索筛选
- **播放列表**：打开视频自动加载同目录全部视频（可选递归、自然排序）
- **网页视频下载**：浏览器扩展插件（MV3，Chrome / Edge）抓取网页视频，默认最高清优先；yt-dlp 解析 + FFmpeg 拉流/合并/转码，桌面本地服务承接，完成自动入媒体库
- **FFmpeg 综合能力**：网络流缓存与边下边播、断点续播；本地与下载后转码（封装互转、编码转换、压缩、音频提取重编码）

## 里程碑

- M0 启动验证（0–2 周）：仓库搭建 + POC
- M1 MVP（2–6 周）：基础播放、快捷键、外挂字幕、播放列表
- M2 媒体库（6–12 周）：扫描索引、封面墙、缩略图、历史
- M3 高级特性（12–20 周）：硬解、网页下载（扩展）、FFmpeg 缓存与转码
- M4 发布（20–24 周）：安装包、更新、日志、多语言

## 构建

- 环境：Visual Studio 2022+ / .NET 8 SDK，Windows 10/11
- 命令：`dotnet build`（解决方案根目录，解决方案文件为 `LocalVideoPlayer.slnx`）

### libmpv 运行时

`libmpv-2.dll`（约 121 MB，静态集成 FFmpeg）**不随仓库分发**（已 gitignore），需要单独获取：

1. 从 [mpv-winbuild-cmake 发布页](https://github.com/shinchiro/mpv-winbuild-cmake/releases/latest) 下载 `mpv-dev-x86_64-*.7z`
2. 解压后将 `libmpv-2.dll` 放到仓库根目录 `libs\` 下
3. 重新 `dotnet build`（csproj 会自动复制到输出目录）

### 冒烟自检

```powershell
# 播放样例视频（本地验证，需先完成 libmpv 放置）
dotnet run --project src/LocalVideoPlayer -- --smoke samples/bbb.mp4
```

示例视频仅存在于本地仓库（未随开源仓库分发，可自行下载任意小体积 mp4 替换）。冒烟成功会在程序目录生成 `smoke_ok.txt`（内容为 `loaded:<路径>`）；失败生成 `smoke_fail.txt` 并写明原因。

## 许可

GPL-3.0，与 mpv 生态一致。详见 [LICENSE](LICENSE)。

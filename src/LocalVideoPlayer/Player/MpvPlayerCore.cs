using System;
using System.Runtime.InteropServices;
using System.Threading;
using LocalVideoPlayer.Native;

namespace LocalVideoPlayer.Player;

/// <summary>
/// 自写 libmpv 播放封装（POC A）：
/// 创建句柄 → 设置 vo=gpu + wid 嵌入宿主窗口 → 事件循环线程 → 加载/播放/暂停/停止。
/// 不依赖 Mpv.NET NuGet（其 1.1.1 依赖已从新版 libmpv 移除的 mpv_detach_destroy）。
/// </summary>
public sealed class MpvPlayerCore : IDisposable
{
    private IntPtr _mpv;
    private Thread? _eventThread;
    private volatile bool _running;

    /// <summary>媒体文件成功加载（MPV_EVENT_FILE_LOADED）。</summary>
    public event EventHandler? FileLoaded;

    /// <summary>播放结束（MPV_EVENT_END_FILE）。</summary>
    public event EventHandler? PlaybackFinished;

    /// <summary>mpv 初始化/命令错误。</summary>
    public event EventHandler<MpvErrorEventArgs>? Error;

    public bool IsInitialized => _mpv != IntPtr.Zero;

    public MpvPlayerCore(IntPtr hwnd)
    {
        _mpv = LibMpvNative.mpv_create();
        if (_mpv == IntPtr.Zero)
            throw new InvalidOperationException("mpv_create 失败：无法创建 mpv 句柄。");

        Check(LibMpvNative.mpv_set_option_string(_mpv, "wid", hwnd.ToString()), "wid");
        Check(LibMpvNative.mpv_set_option_string(_mpv, "vo", "gpu"), "vo=gpu");

        Check(LibMpvNative.mpv_initialize(_mpv), "initialize");

        Check(LibMpvNative.mpv_request_event(_mpv, LibMpvNative.EventId.FileLoaded, 1), "request FileLoaded");
        Check(LibMpvNative.mpv_request_event(_mpv, LibMpvNative.EventId.EndFile, 1), "request EndFile");

        _running = true;
        _eventThread = new Thread(EventLoop) { IsBackground = true, Name = "mpv-event-loop" };
        _eventThread.Start();
    }

    public void Load(string path)
    {
        Check(Command("loadfile", path, "replace"), $"loadfile {path}");
    }

    public void Pause() => SetProperty("pause", "yes");

    public void Resume() => SetProperty("pause", "no");

    public void Stop() => Check(Command("stop"), "stop");

    public void SetVolume(int volume) => SetProperty("volume", volume.ToString());

    private int Command(params string[] args)
    {
        IntPtr[] ptrs = new IntPtr[args.Length + 1];
        GCHandle handle = GCHandle.Alloc(ptrs, GCHandleType.Pinned);
        try
        {
            for (int i = 0; i < args.Length; i++)
                ptrs[i] = Marshal.StringToCoTaskMemUTF8(args[i]);
            ptrs[args.Length] = IntPtr.Zero;
            return LibMpvNative.mpv_command(_mpv, handle.AddrOfPinnedObject());
        }
        finally
        {
            for (int i = 0; i < args.Length; i++)
                Marshal.FreeCoTaskMem(ptrs[i]);
            handle.Free();
        }
    }

    private void SetProperty(string name, string value)
    {
        Check(LibMpvNative.mpv_set_property_string(_mpv, name, value), $"{name}={value}");
    }

    private void EventLoop()
    {
        while (_running)
        {
            IntPtr p = LibMpvNative.mpv_wait_event(_mpv, -1);
            if (p == IntPtr.Zero)
                continue;

            var evt = Marshal.PtrToStructure<LibMpvNative.MpvEvent>(p);
            switch (evt.EventId)
            {
                case LibMpvNative.EventId.FileLoaded:
                    FileLoaded?.Invoke(this, EventArgs.Empty);
                    break;
                case LibMpvNative.EventId.EndFile:
                    PlaybackFinished?.Invoke(this, EventArgs.Empty);
                    break;
                case LibMpvNative.EventId.Shutdown:
                    return;
            }
        }
    }

    private void Check(int error, string what)
    {
        if (error < 0)
        {
            IntPtr msg = LibMpvNative.mpv_error_string(error);
            string text = msg == IntPtr.Zero ? "未知错误" : Marshal.PtrToStringUTF8(msg) ?? "未知错误";
            Error?.Invoke(this, new MpvErrorEventArgs(what, text));
            throw new InvalidOperationException($"mpv {what} 失败：{text}");
        }
    }

    public void Dispose()
    {
        _running = false;
        if (_mpv != IntPtr.Zero)
        {
            LibMpvNative.mpv_destroy(_mpv);
            _mpv = IntPtr.Zero;
        }
        _eventThread = null;
    }
}

public sealed class MpvErrorEventArgs : EventArgs
{
    public string Operation { get; }
    public string Message { get; }

    public MpvErrorEventArgs(string operation, string message)
    {
        Operation = operation;
        Message = message;
    }
}

using System;
using System.Runtime.InteropServices;

namespace LocalVideoPlayer.Native;

/// <summary>
/// libmpv client API 薄 P/Invoke 绑定（依据官方 client.h）。
/// 仅声明本项目实际使用的函数；渲染层（mpv_render_*）在 POC B 引入。
/// </summary>
internal static class LibMpvNative
{
    private const string Dll = "libmpv-2.dll";

    [DllImport(Dll, CallingConvention = CallingConvention.Cdecl)]
    internal static extern IntPtr mpv_create();

    [DllImport(Dll, CallingConvention = CallingConvention.Cdecl)]
    internal static extern int mpv_initialize(IntPtr mpv);

    [DllImport(Dll, CallingConvention = CallingConvention.Cdecl)]
    internal static extern int mpv_set_option_string(IntPtr mpv, string name, string value);

    [DllImport(Dll, CallingConvention = CallingConvention.Cdecl)]
    internal static extern int mpv_set_property_string(IntPtr mpv, string name, string value);

    [DllImport(Dll, CallingConvention = CallingConvention.Cdecl)]
    internal static extern int mpv_command(IntPtr mpv, IntPtr args);

    [DllImport(Dll, CallingConvention = CallingConvention.Cdecl)]
    internal static extern int mpv_request_event(IntPtr mpv, int eventId, int enable);

    [DllImport(Dll, CallingConvention = CallingConvention.Cdecl)]
    internal static extern IntPtr mpv_wait_event(IntPtr mpv, double timeout);

    [DllImport(Dll, CallingConvention = CallingConvention.Cdecl)]
    internal static extern IntPtr mpv_error_string(int error);

    [DllImport(Dll, CallingConvention = CallingConvention.Cdecl)]
    internal static extern void mpv_destroy(IntPtr mpv);

    /// <summary>MPV_EVENT_* 枚举（client.h 现行版本，小数字序号）。</summary>
    internal static class EventId
    {
        public const int Shutdown = 1;
        public const int StartFile = 6;
        public const int EndFile = 7;
        public const int FileLoaded = 8;
    }

    /// <summary>mpv_event 结构（顺序布局即可）。</summary>
    [StructLayout(LayoutKind.Sequential)]
    internal struct MpvEvent
    {
        public int EventId;
        public int Error;
        public ulong ReplyUserdata;
        public IntPtr Data;
    }
}

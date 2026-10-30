using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;

namespace LocalVideoPlayer;

/// <summary>
/// 承载 libmpv 视频输出的子窗口宿主（WPF HwndHost）。
/// mpv 通过 <c>wid</c> 选项将画面直接绘制到该 HWND（gpu vo），
/// 这是 POC A 阶段最简的嵌入方式；后续 POC B 将替换为 D3D11 render API + 交换链。
/// </summary>
public sealed class MpvHost : HwndHost
{
    /// <summary>子窗口句柄，供 mpv 嵌入（MpvPlayerCore 构造传入）。</summary>
    public IntPtr PlayerHwnd => Handle;

    /// <summary>窗口创建完成（HWND 可用）时触发。</summary>
    public event EventHandler? WindowReady;

    private const int WS_CHILD = 0x40000000;
    private const int WS_VISIBLE = 0x10000000;

    protected override HandleRef BuildWindowCore(HandleRef hwndParent)
    {
        IntPtr hwnd = CreateWindowEx(
            0, "static", "",
            WS_CHILD | WS_VISIBLE,
            0, 0, 1, 1,
            hwndParent.Handle, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero);

        if (hwnd == IntPtr.Zero)
            throw new InvalidOperationException("创建 mpv 宿主子窗口失败。");

        Dispatcher.BeginInvoke(() => WindowReady?.Invoke(this, EventArgs.Empty));
        return new HandleRef(this, hwnd);
    }

    protected override void DestroyWindowCore(HandleRef hwnd)
    {
        DestroyWindow(hwnd.Handle);
    }

    [DllImport("user32.dll", SetLastError = true)]
    private static extern IntPtr CreateWindowEx(int exStyle, string className, string windowName,
        int style, int x, int y, int width, int height, IntPtr parent, IntPtr menu, IntPtr instance, IntPtr param);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool DestroyWindow(IntPtr hwnd);
}

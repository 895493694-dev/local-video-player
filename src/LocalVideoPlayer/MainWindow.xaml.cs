using System;
using System.IO;
using System.Windows;
using System.Windows.Threading;
using LocalVideoPlayer.Player;
using Microsoft.Win32;

namespace LocalVideoPlayer;

/// <summary>
/// POC A：libmpv 播放首个本地视频（自写绑定 + gpu vo 嵌入 HwndHost）。
/// 命令行 --smoke &lt;file&gt;：自动化冒烟——加载成功写 smoke_ok.txt 并退出（用于 CI/自检）。
/// </summary>
public partial class MainWindow : Window
{
    private MpvPlayerCore? _player;
    private string _currentFile = "";

    public MainWindow()
    {
        InitializeComponent();
        Loaded += MainWindow_Loaded;
        Closing += MainWindow_Closing;
    }

    private void MainWindow_Loaded(object? sender, RoutedEventArgs e)
    {
        VideoHost.WindowReady += VideoHost_WindowReady;

        var args = Environment.GetCommandLineArgs();
        int i = Array.IndexOf(args, "--smoke");
        if (i >= 0 && i + 1 < args.Length && File.Exists(args[i + 1]))
            _currentFile = args[i + 1];
        else if (i >= 0)
        {
            File.WriteAllText("smoke_fail.txt", "缺少有效视频文件参数: --smoke <file>");
            ShutdownApplication();
        }
    }

    private void VideoHost_WindowReady(object? sender, EventArgs e)
    {
        try
        {
            _player = new MpvPlayerCore(VideoHost.PlayerHwnd);
            _player.FileLoaded += (_, _) =>
            {
                Dispatcher.Invoke(() => StatusText.Text = "已加载媒体，播放中");
                if (IsSmokeMode())
                    SmokeSuccess();
            };
            _player.PlaybackFinished += (_, _) =>
                Dispatcher.Invoke(() => StatusText.Text = "播放结束");
            _player.Error += (_, ex) =>
                Dispatcher.Invoke(() => StatusText.Text = "mpv 错误：" + ex.Message);
        }
        catch (Exception ex)
        {
            StatusText.Text = "初始化失败：" + ex.Message;
            if (IsSmokeMode())
                SmokeFail("初始化失败: " + ex.Message);
            return;
        }

        if (_currentFile.Length > 0)
        {
            StatusText.Text = "正在加载：" + Path.GetFileName(_currentFile);
            LoadCurrentFile();
        }
    }

    private void LoadCurrentFile()
    {
        if (_player == null || _currentFile.Length == 0)
            return;
        try
        {
            _player.Load(_currentFile);
        }
        catch (Exception ex)
        {
            StatusText.Text = "加载失败：" + ex.Message;
            if (IsSmokeMode())
                SmokeFail("加载失败: " + ex.Message);
        }
    }

    private void OpenButton_Click(object sender, RoutedEventArgs e)
    {
        var dlg = new OpenFileDialog
        {
            Title = "选择视频文件",
            Filter = "视频文件|*.mp4;*.mkv;*.avi;*.mov;*.webm;*.flv;*.ts;*.wmv;*.m4v|所有文件|*.*"
        };
        if (dlg.ShowDialog(this) == true)
        {
            _currentFile = dlg.FileName;
            LoadCurrentFile();
        }
    }

    private void PlayButton_Click(object sender, RoutedEventArgs e)
    {
        if (_player == null) return;
        _player.Resume();
        StatusText.Text = "播放中";
    }

    private void PauseButton_Click(object sender, RoutedEventArgs e)
    {
        if (_player == null) return;
        _player.Pause();
        StatusText.Text = "已暂停";
    }

    private void StopButton_Click(object sender, RoutedEventArgs e)
    {
        if (_player == null) return;
        _player.Stop();
        StatusText.Text = "已停止";
    }

    private void VolumeSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (VolumeText != null)
            VolumeText.Text = ((int)VolumeSlider.Value).ToString() + "%";
        if (_player != null)
            _player.SetVolume((int)VolumeSlider.Value);
    }

    private void MainWindow_Closing(object? sender, System.ComponentModel.CancelEventArgs e)
    {
        _player?.Dispose();
    }

    private bool IsSmokeMode() => Environment.GetCommandLineArgs().Contains("--smoke");

    private void SmokeSuccess()
    {
        File.WriteAllText("smoke_ok.txt", $"loaded:{_currentFile}");
        ShutdownApplication();
    }

    private void SmokeFail(string reason)
    {
        File.WriteAllText("smoke_fail.txt", reason);
        ShutdownApplication();
    }

    private static void ShutdownApplication()
    {
        Application.Current.Dispatcher.BeginInvoke(
            DispatcherPriority.Background,
            new Action(() => Application.Current.Shutdown()));
    }
}

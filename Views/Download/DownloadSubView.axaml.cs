using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Avalonia.Threading;
using MinecraftLaunch.Base.Models.Network;
using MinecraftLaunch.Components.Installer;
using PCL.Neo_InML.Controls;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace PCL.Neo_InML.Views.Download;

public partial class DownloadSubView : UserControl
{
    // 版本清单有一百多 KB 且很少变，缓存一次避免每次切页都重新请求
    private static List<VersionManifestEntry>? _cachedVersions;
    private static Bitmap? _releaseIcon;
    private static Bitmap? _snapshotIcon;

    private bool _loaded;

    // ---- 非线性滚动 ----
    private const double ScrollDurationMs = 350;
    private const double WheelStep = 120;

    private readonly Stopwatch _scrollStopwatch = new();
    private readonly DispatcherTimer _scrollTimer;
    private double _fromY;
    private double _toY;
    private double _targetOffsetY = double.NaN;
    private bool _isScrolling;
    private PointerWheelEventArgs? _handledWheelEvent;

    public DownloadSubView()
    {
        InitializeComponent();

        _scrollTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(16) };
        _scrollTimer.Tick += OnScrollTick;

        Loaded += async (_, _) =>
        {
            if (_loaded)
                return;

            _loaded = true;
            await LoadVersionsAsync();
        };

        // 关键：用隧道阶段 + handledEventsToo 拦截，
        // 否则 ScrollViewer 自身的类处理器会先把事件标记为已处理，我们的 handler 根本不会被调用。
        PanList.AddHandler(
            PointerWheelChangedEvent,
            OnScrollWheelChanged,
            RoutingStrategies.Tunnel | RoutingStrategies.Bubble,
            handledEventsToo: true);
    }

    /// <summary>
    /// 拉取版本清单并填充三个分组。
    /// </summary>
    private async Task LoadVersionsAsync()
    {
        try
        {
            _cachedVersions ??= (await VanillaInstaller.EnumerableMinecraftAsync(CancellationToken.None))
                .ToList();

            var versions = _cachedVersions;

            var releases = versions
                .Where(x => string.Equals(x.Type, "release", StringComparison.OrdinalIgnoreCase))
                .ToList();
            var snapshots = versions
                .Where(x => string.Equals(x.Type, "snapshot", StringComparison.OrdinalIgnoreCase))
                .ToList();

            _releaseIcon ??= LoadBlockIcon("Grass.png");
            _snapshotIcon ??= LoadBlockIcon("CommandBlock.png");

            ItemsLatest.ItemsSource = BuildLatestRows(releases, snapshots);
            ItemsRelease.ItemsSource = releases.Select(x => ToRow(x, _releaseIcon, null)).ToList();
            ItemsSnapshot.ItemsSource = snapshots.Select(x => ToRow(x, _snapshotIcon, null)).ToList();

            PanLoading.IsVisible = false;
            PanList.IsVisible = true;
        }
        catch (Exception ex)
        {
            PanLoading.State = MyLoading.LoadingState.Error;
            PanLoading.TextError = $"获取版本列表失败：{ex.Message}";
        }
    }

    /// <summary>
    /// 「最新版本」卡片：最新正式版 + 最新预览版。
    /// </summary>
    private List<VersionRow> BuildLatestRows(List<VersionManifestEntry> releases, List<VersionManifestEntry> snapshots)
    {
        var rows = new List<VersionRow>();

        if (releases.Count > 0)
            rows.Add(ToRow(releases[0], _releaseIcon, "最新正式版"));

        if (snapshots.Count > 0)
            rows.Add(ToRow(snapshots[0], _snapshotIcon, "最新预览版"));

        return rows;
    }

    private static VersionRow ToRow(VersionManifestEntry entry, IImage? icon, string? latestLabel)
    {
        var time = entry.ReleaseTime.ToString("yyyy/MM/dd HH:mm");

        return new VersionRow
        {
            Name = entry.McVersion,
            Subtitle = latestLabel is null ? time : $"{latestLabel}，发布于 {time}",
            Icon = icon
        };
    }

    private static Bitmap LoadBlockIcon(string fileName)
    {
        return new Bitmap(AssetLoader.Open(new Uri($"avares://PCL.Neo_InML/Assets/Blocks/{fileName}")));
    }

    /// <summary>
    /// 处理滚轮事件，启动非线性滚动动画。
    /// </summary>
    private void OnScrollWheelChanged(object? sender, PointerWheelEventArgs e)
    {
        // 隧道 + 冒泡各会触发一次，同一次事件只处理一遍
        if (ReferenceEquals(e, _handledWheelEvent))
            return;

        _handledWheelEvent = e;

        var maxOffset = Math.Max(0, PanList.Extent.Height - PanList.Viewport.Height);

        if (maxOffset <= 0)
            return;

        // 上次动画结束后位置若被外部改过（拖滚动条等），重置累加目标
        if (!_isScrolling && !double.IsNaN(_targetOffsetY) && Math.Abs(PanList.Offset.Y - _targetOffsetY) > 1)
            _targetOffsetY = PanList.Offset.Y;

        // 连续滚动时以"目标值"累加，否则追不上手速
        var goal = double.IsNaN(_targetOffsetY) ? PanList.Offset.Y : _targetOffsetY;
        var target = Math.Clamp(goal - e.Delta.Y * WheelStep, 0, maxOffset);

        _targetOffsetY = target;

        // 从当前位置起步，动画中途再来一格也能平滑接上
        _fromY = PanList.Offset.Y;
        _toY = target;
        _scrollStopwatch.Restart();

        if (!_isScrolling)
        {
            _isScrolling = true;
            _scrollTimer.Start();
        }

        // 拦下事件，避免 ScrollViewer 再走一遍原生滚动
        e.Handled = true;
    }

    /// <summary>
    /// 每帧按 QuinticEaseOut 推进：起步快、收尾慢。
    /// </summary>
    private void OnScrollTick(object? sender, EventArgs e)
    {
        var progress = Math.Clamp(_scrollStopwatch.Elapsed.TotalMilliseconds / ScrollDurationMs, 0, 1);
        var eased = 1 - Math.Pow(1 - progress, 5);

        PanList.Offset = new Vector(PanList.Offset.X, _fromY + (_toY - _fromY) * eased);

        if (progress < 1)
            return;

        PanList.Offset = new Vector(PanList.Offset.X, _toY);
        _isScrolling = false;
        _scrollTimer.Stop();
    }
}

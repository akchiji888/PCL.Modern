using Avalonia.Media;

namespace PCL.Neo_InML.Views.Download;

/// <summary>
/// 版本列表里的一行。
/// </summary>
public sealed class VersionRow
{
    /// <summary>
    /// 版本号，如 26.3。
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// 副标题，如「发布于 2026/09/15 19:23」。
    /// </summary>
    public required string Subtitle { get; init; }

    /// <summary>
    /// 左侧方块图标（正式版是草方块，预览版是命令方块）。
    /// </summary>
    public required IImage? Icon { get; init; }
}

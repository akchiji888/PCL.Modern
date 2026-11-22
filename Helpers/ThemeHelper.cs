using Avalonia;
using Avalonia.Media;
using Avalonia.Styling;
using PCL.Neo_InML.Models;

namespace PCL.Neo_InML.Helpers;

/// <summary>
/// 主题色管理。
/// 迁移到 MVC 后这里不再持有 MainWindow 引用：它只提供调色板和画刷工厂，
/// 具体把画刷贴到哪个控件上，由 View / Controller 自己决定。
/// </summary>
public static class ThemeHelper
{
    public static MyColor Color1 { get; } = new(52, 61, 74);
    public static MyColor Color2 { get; } = new(11, 91, 203);
    public static MyColor Color3 { get; } = new(19, 112, 243);
    public static MyColor Color4 { get; } = new(72, 144, 245);
    public static MyColor Color5 { get; } = new(150, 192, 249);
    public static MyColor Color6 { get; } = new(213, 230, 253);
    public static MyColor Color7 { get; } = new(224, 237, 253);
    public static MyColor Color8 { get; } = new(234, 242, 254);
    public static MyColor ColorBg0 { get; } = new(150, 192, 249);
    public static MyColor ColorBg1 { get; } = new(190, new MyColor(224, 237, 253));
    public static MyColor ColorGray1 { get; } = new(64, 64, 64);
    public static MyColor ColorGray2 { get; } = new(115, 115, 115);
    public static MyColor ColorGray3 { get; } = new(140, 140, 140);
    public static MyColor ColorGray4 { get; } = new(166, 166, 166);
    public static MyColor ColorGray5 { get; } = new(204, 204, 204);
    public static MyColor ColorGray6 { get; } = new(235, 235, 235);
    public static MyColor ColorGray7 { get; } = new(240, 240, 240);
    public static MyColor ColorGray8 { get; } = new(245, 245, 245);
    public static MyColor ColorSemiTransparent { get; } = new(1, new MyColor(234, 242, 254));

    /// <summary>
    /// 标题栏渐变（横向，两端略深）。
    /// </summary>
    public static LinearGradientBrush CreateTitleBrush()
    {
        var brush = new LinearGradientBrush
        {
            EndPoint = new RelativePoint(1, 0, RelativeUnit.Relative),
            StartPoint = new RelativePoint(0, 0, RelativeUnit.Relative)
        };

        brush.GradientStops.Add(new GradientStop { Offset = 0, Color = Color3 * 0.90 });
        brush.GradientStops.Add(new GradientStop { Offset = 0.5, Color = Color3 });
        brush.GradientStops.Add(new GradientStop { Offset = 1, Color = Color3 * 0.90 });

        return brush;
    }

    /// <summary>
    /// 窗口背景渐变。
    /// </summary>
    public static LinearGradientBrush CreateBackgroundBrush(ThemeVariant themeVariant)
    {
        var isDark = themeVariant == ThemeVariant.Dark;

        var light = Color6;
        var middle = isDark ? Color1 * 0.90 : Color8;
        var edge = isDark ? Color1 * 0.80 : Color7;

        var brush = new LinearGradientBrush
        {
            EndPoint = new RelativePoint(0.1, 1, RelativeUnit.Relative),
            StartPoint = new RelativePoint(0.9, 0, RelativeUnit.Relative)
        };

        brush.GradientStops.Add(new GradientStop { Offset = -0.1, Color = isDark ? Color1 * 0.75 : light });
        brush.GradientStops.Add(new GradientStop { Offset = 0.4, Color = middle });
        brush.GradientStops.Add(new GradientStop { Offset = 1.1, Color = edge });

        return brush;
    }
}

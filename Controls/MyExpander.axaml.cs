using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Shapes;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;

namespace PCL.Neo_InML.Controls;

/// <summary>
/// PCL 风格的可折叠卡片：圆角白底 + 标题 + 右上角箭头，点标题栏展开/收起。
/// 悬停时标题变主题色并出现主题色光晕（样式见 Controls/MyExpander.axaml）。
/// </summary>
public class MyExpander : Expander
{
    private Path? _chevron;
    private Border? _header;

    public MyExpander()
    {
        // 只在构造函数里挂一次，避免 OnApplyTemplate 重复执行导致多次挂载
        AddHandler(PointerReleasedEvent, OnHeaderPointerReleased, RoutingStrategies.Bubble);
    }

    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        base.OnApplyTemplate(e);

        _chevron = e.NameScope.Find<Path>("Chevron");
        _header = e.NameScope.Find<Border>("PART_Header");

        UpdateChevron();
    }

    /// <summary>
    /// 只有点在标题栏区域内才切换展开状态，点内容区不响应。
    /// </summary>
    private void OnHeaderPointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        if (e.InitialPressMouseButton != MouseButton.Left || _header is null)
            return;

        var position = e.GetPosition(_header);
        var bounds = _header.Bounds;

        if (position.X < 0 || position.Y < 0 || position.X > bounds.Width || position.Y > bounds.Height)
            return;

        IsExpanded = !IsExpanded;
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == IsExpandedProperty)
            UpdateChevron();
    }

    /// <summary>
    /// 收起时箭头朝下（点击展开），展开时朝上（点击收起）。
    /// </summary>
    private void UpdateChevron()
    {
        if (_chevron?.RenderTransform is RotateTransform rotate)
            rotate.Angle = IsExpanded ? 180 : 0;
    }
}

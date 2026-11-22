using Avalonia;
using Avalonia.Animation.Easings;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Avalonia.Rendering.Composition;
using Avalonia.Rendering.Composition.Animations;
using Avalonia.Styling;
using PCL.Neo_InML.Animations.Easings;
using PCL.Neo_InML.Controls;
using PCL.Neo_InML.Helpers;
using PCL.Neo_InML.Views.Home;
using PCL.Neo_InML.Views.Online;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace PCL.Neo_InML;

public partial class MainWindow : Window
{
    private CompositionVisual? _leftNavigationControlVisual;
    private CompositionVisual? _rightNavigationControlVisual;
    private Compositor? _compositor;

    // TODO(MVC): 导航状态应该搬到 NavigationController 里，这里只是先让外壳能跑
    private readonly Stack<int> _history = [];
    private int _currentTag;

    public MainWindow()
    {
        InitializeComponent();

        NavBackgroundBorder.PointerPressed += (i, e) =>
        {
            if (e.GetCurrentPoint(i as Control).Properties.IsLeftButtonPressed)
            {
                BeginMoveDrag(e);
            }
        };

        if (!RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            SetupSide("Left", StandardCursorType.LeftSide, WindowEdge.West);
            SetupSide("Right", StandardCursorType.RightSide, WindowEdge.East);
            SetupSide("Top", StandardCursorType.TopSide, WindowEdge.North);
            SetupSide("Bottom", StandardCursorType.BottomSide, WindowEdge.South);
            SetupSide("TopLeft", StandardCursorType.TopLeftCorner, WindowEdge.NorthWest);
            SetupSide("TopRight", StandardCursorType.TopRightCorner, WindowEdge.NorthEast);
            SetupSide("BottomLeft", StandardCursorType.BottomLeftCorner, WindowEdge.SouthWest);
            SetupSide("BottomRight", StandardCursorType.BottomRightCorner, WindowEdge.SouthEast);
        }

        if (Application.Current is { } app)
        {
            ApplyTheme(app.ActualThemeVariant);
            app.ActualThemeVariantChanged += (_, _) => ApplyTheme(app.ActualThemeVariant);
        }

        BtnTitleClose.Click += async (_, _) =>
        {
            AnimationOut();
            await Task.Delay(180);
            Close();
        };

        BtnTitleMin.Click += (_, _) => WindowState = WindowState.Minimized;

        LeftNavigationControl.Loaded += (_, _) =>
        {
            LeftNavigationControlBorder.Width = LeftNavigationControl.Bounds.Width;

            LeftNavigationControl.SizeChanged += (_, args) =>
            {
                if (args.WidthChanged)
                    LeftNavigationControlBorder.Width = args.NewSize.Width;
            };

            // 获取导航控件的 CompositionVisual，用于切页动画
            _leftNavigationControlVisual = ElementComposition.GetElementVisual(LeftNavigationControl);
            _rightNavigationControlVisual = ElementComposition.GetElementVisual(RightNavigationControl);

            if (_leftNavigationControlVisual != null)
            {
                _compositor = _leftNavigationControlVisual.Compositor;
            }
        };

        GridRoot.Opacity = 0; // 在此处初始化透明度，不然将闪现
        Loaded += (_, _) =>
        {
            AnimationIn();
            // 构造函数里还没布局完，导向首页放在 Loaded 里
            NavigateTo(1, false);
        };
    }

    #region 导航

    private void OnNavButtonClick(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (sender is Control { Tag: string tagText } && int.TryParse(tagText, out var tag))
        {
            NavigateTo(tag, true);
        }
    }

    private void OnBackClick(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (_history.Count == 0)
            return;

        var tag = _history.Pop();
        NavigateTo(tag, false);
        BtnNavBack.IsEnabled = _history.Count > 0;
    }

    /// <summary>
    /// 切换到指定导航页。
    /// </summary>
    /// <param name="tag">导航按钮的 Tag（1 首页 / 2 下载 / 3 联机 / 4 设置 / 5 更多）。</param>
    /// <param name="pushHistory">是否把当前页压入返回栈。</param>
    private void NavigateTo(int tag, bool pushHistory)
    {
        var changed = tag != _currentTag;

        if (pushHistory && _currentTag != 0 && changed)
            _history.Push(_currentTag);

        _currentTag = tag;
        SetNavChecked(tag);

        switch (tag)
        {
            case 1:
                LeftNavigationControl.Content = new HomeView();
                RightNavigationControl.Content = new HomeSubView();
                break;
            case 3:
                LeftNavigationControl.Content = new OnlineView();
                RightNavigationControl.Content = new OnlineSubView();
                break;
            default:
                // TODO: 下载 / 设置 / 更多 三个页面还没实现
                break;
        }

        BtnNavBack.IsEnabled = _history.Count > 0;

        if (changed)
            PlayNavigationAnimation(pushHistory);
    }

    private void SetNavChecked(int tag)
    {
        MyRadioButton[] buttons =
        [
            BtnTitleSelect0, BtnTitleSelect1, BtnTitleSelect2, BtnTitleSelect3, BtnTitleSelect4
        ];

        for (var i = 0; i < buttons.Length; i++)
        {
            buttons[i].IsChecked = i + 1 == tag;
        }
    }

    #endregion

    #region 主题

    private void ApplyTheme(ThemeVariant themeVariant)
    {
        NavBackgroundBorder.Background = ThemeHelper.CreateTitleBrush();
        MainBorder.Background = ThemeHelper.CreateBackgroundBrush(themeVariant);
        ImgTitle.Source = new Bitmap(AssetLoader.Open(new Uri("avares://PCL.Neo_InML/Assets/Themes/8.png")));
    }

    #endregion

    #region 窗口缩放

    private void SetupSide(string name, StandardCursorType cursor, WindowEdge edge)
    {
        var ctl = this.Get<Control>(name);
        ctl.Cursor = new Cursor(cursor);
        ctl.PointerPressed += (i, e) =>
        {
            if (e.GetCurrentPoint(i as Control).Properties.IsLeftButtonPressed)
            {
                BeginResizeDrag(edge, e);
            }
        };
    }

    #endregion

    #region 动画

    /// <summary>
    /// 进入窗口的动画。
    /// </summary>
    private async void AnimationIn()
    {
        await Task.Delay(100);

        var mainWindowCompositionVisual = ElementComposition.GetElementVisual(GridRoot);
        if (mainWindowCompositionVisual is null)
            return;

        var compositor = mainWindowCompositionVisual.Compositor;

        var opacityFrameAnimation = compositor.CreateScalarKeyFrameAnimation();
        opacityFrameAnimation.Duration = TimeSpan.FromMilliseconds(250);
        opacityFrameAnimation.InsertKeyFrame(0f, 0f, new CubicEaseOut());
        opacityFrameAnimation.InsertKeyFrame(1f, 1f, new CubicEaseOut());
        opacityFrameAnimation.Target = "Opacity";

        var rotateTransformAngleAnimation = compositor.CreateScalarKeyFrameAnimation();
        rotateTransformAngleAnimation.Duration = TimeSpan.FromMilliseconds(500);
        rotateTransformAngleAnimation.InsertKeyFrame(0f, -0.06f, new MyBackEaseOut(EasePower.Weak));
        rotateTransformAngleAnimation.InsertKeyFrame(1f, 0f, new MyBackEaseOut(EasePower.Weak));
        rotateTransformAngleAnimation.Target = "RotationAngle";

        var translateTransformYAnimation = compositor.CreateVector3KeyFrameAnimation();
        translateTransformYAnimation.Duration = TimeSpan.FromMilliseconds(600);
        translateTransformYAnimation.InsertKeyFrame(0f, new Vector3(0f, 60f, 0f), new MyBackEaseOut(EasePower.Weak));
        translateTransformYAnimation.InsertKeyFrame(1f, new Vector3(0f, 0f, 0f), new MyBackEaseOut(EasePower.Weak));
        translateTransformYAnimation.Target = "Offset";

        var animationGroup = compositor.CreateAnimationGroup();
        animationGroup.Add(opacityFrameAnimation);
        animationGroup.Add(rotateTransformAngleAnimation);
        animationGroup.Add(translateTransformYAnimation);

        var size = mainWindowCompositionVisual.Size;
        mainWindowCompositionVisual.CenterPoint = new Vector3D((float)size.X / 2, (float)size.Y / 2,
            (float)mainWindowCompositionVisual.CenterPoint.Z);

        mainWindowCompositionVisual.StartAnimationGroup(animationGroup);
    }

    /// <summary>
    /// 关闭窗口的动画。
    /// </summary>
    private void AnimationOut()
    {
        var mainWindowCompositionVisual = ElementComposition.GetElementVisual(GridRoot);
        if (mainWindowCompositionVisual is null)
            return;

        var compositor = mainWindowCompositionVisual.Compositor;

        var opacityFrameAnimation = compositor.CreateScalarKeyFrameAnimation();
        opacityFrameAnimation.DelayBehavior = AnimationDelayBehavior.SetInitialValueBeforeDelay;
        opacityFrameAnimation.DelayTime = TimeSpan.FromMilliseconds(40);
        opacityFrameAnimation.Duration = TimeSpan.FromMilliseconds(140);
        opacityFrameAnimation.InsertKeyFrame(0f, 1f, new QuadraticEaseOut());
        opacityFrameAnimation.InsertKeyFrame(1f, 0f, new QuadraticEaseOut());
        opacityFrameAnimation.Target = "Opacity";

        var rotateTransformAngleAnimation = compositor.CreateScalarKeyFrameAnimation();
        rotateTransformAngleAnimation.Duration = TimeSpan.FromMilliseconds(180);
        rotateTransformAngleAnimation.InsertKeyFrame(0f, 0f, new QuadraticEaseOut());
        rotateTransformAngleAnimation.InsertKeyFrame(1f, 0.0006f, new QuadraticEaseOut());
        rotateTransformAngleAnimation.Target = "RotationAngle";

        var translateTransformYAnimation = compositor.CreateVector3KeyFrameAnimation();
        translateTransformYAnimation.Duration = TimeSpan.FromMilliseconds(180);
        translateTransformYAnimation.InsertKeyFrame(0f, new Vector3(0f, 0f, 0f), new QuadraticEaseOut());
        translateTransformYAnimation.InsertKeyFrame(1f, new Vector3(0f, 20f, 0f), new QuadraticEaseOut());
        translateTransformYAnimation.Target = "Offset";

        var scaleTransformAnimation = compositor.CreateVector3KeyFrameAnimation();
        scaleTransformAnimation.Duration = TimeSpan.FromMilliseconds(180);
        scaleTransformAnimation.InsertKeyFrame(0f, new Vector3(1f, 1f, 1f), new QuadraticEaseOut());
        scaleTransformAnimation.InsertKeyFrame(1f, new Vector3(0.88f, 0.88f, 1f), new QuadraticEaseOut());
        scaleTransformAnimation.Target = "Scale";

        var animationGroup = compositor.CreateAnimationGroup();
        animationGroup.Add(opacityFrameAnimation);
        animationGroup.Add(rotateTransformAngleAnimation);
        animationGroup.Add(translateTransformYAnimation);
        animationGroup.Add(scaleTransformAnimation);

        var size = mainWindowCompositionVisual.Size;
        mainWindowCompositionVisual.CenterPoint = new Vector3D((float)size.X / 2, (float)size.Y / 2,
            (float)mainWindowCompositionVisual.CenterPoint.Z);

        mainWindowCompositionVisual.StartAnimationGroup(animationGroup);
    }

    private void PlayNavigationAnimation(bool forward)
    {
        if (forward)
            PlayForwardNavigationAnimation();
        else
            PlayBackwardNavigationAnimation();
    }

    private void PlayForwardNavigationAnimation()
    {
        if (_compositor == null || _leftNavigationControlVisual == null || _rightNavigationControlVisual == null)
            return;

        // 左侧面板动画 - 从左滑入
        var leftOffsetAnimation = _compositor.CreateVector3KeyFrameAnimation();
        leftOffsetAnimation.Duration = TimeSpan.FromMilliseconds(300);
        leftOffsetAnimation.InsertKeyFrame(0f, new Vector3(-50f, 0f, 0f), new CubicEaseOut());
        leftOffsetAnimation.InsertKeyFrame(1f, new Vector3(0f, 0f, 0f), new CubicEaseOut());
        leftOffsetAnimation.Target = "Offset";

        var leftOpacityAnimation = _compositor.CreateScalarKeyFrameAnimation();
        leftOpacityAnimation.Duration = TimeSpan.FromMilliseconds(300);
        leftOpacityAnimation.InsertKeyFrame(0f, 0f, new CubicEaseOut());
        leftOpacityAnimation.InsertKeyFrame(1f, 1f, new CubicEaseOut());
        leftOpacityAnimation.Target = "Opacity";

        // 右侧面板动画 - 从右滑入
        var rightOffsetAnimation = _compositor.CreateVector3KeyFrameAnimation();
        rightOffsetAnimation.Duration = TimeSpan.FromMilliseconds(300);
        rightOffsetAnimation.InsertKeyFrame(0f, new Vector3(50f, 0f, 0f), new CubicEaseOut());
        rightOffsetAnimation.InsertKeyFrame(1f, new Vector3(0f, 0f, 0f), new CubicEaseOut());
        rightOffsetAnimation.Target = "Offset";

        var rightOpacityAnimation = _compositor.CreateScalarKeyFrameAnimation();
        rightOpacityAnimation.Duration = TimeSpan.FromMilliseconds(300);
        rightOpacityAnimation.InsertKeyFrame(0f, 0f, new CubicEaseOut());
        rightOpacityAnimation.InsertKeyFrame(1f, 1f, new CubicEaseOut());
        rightOpacityAnimation.Target = "Opacity";

        RunNavigationAnimation(leftOffsetAnimation, leftOpacityAnimation, rightOffsetAnimation, rightOpacityAnimation);
    }

    private void PlayBackwardNavigationAnimation()
    {
        if (_compositor == null || _leftNavigationControlVisual == null || _rightNavigationControlVisual == null)
            return;

        // 左侧面板动画 - 从右滑入
        var leftOffsetAnimation = _compositor.CreateVector3KeyFrameAnimation();
        leftOffsetAnimation.Duration = TimeSpan.FromMilliseconds(300);
        leftOffsetAnimation.InsertKeyFrame(0f, new Vector3(50f, 0f, 0f), new CubicEaseOut());
        leftOffsetAnimation.InsertKeyFrame(1f, new Vector3(0f, 0f, 0f), new CubicEaseOut());
        leftOffsetAnimation.Target = "Offset";

        var leftOpacityAnimation = _compositor.CreateScalarKeyFrameAnimation();
        leftOpacityAnimation.Duration = TimeSpan.FromMilliseconds(300);
        leftOpacityAnimation.InsertKeyFrame(0f, 0f, new CubicEaseOut());
        leftOpacityAnimation.InsertKeyFrame(1f, 1f, new CubicEaseOut());
        leftOpacityAnimation.Target = "Opacity";

        // 右侧面板动画 - 从左滑入
        var rightOffsetAnimation = _compositor.CreateVector3KeyFrameAnimation();
        rightOffsetAnimation.Duration = TimeSpan.FromMilliseconds(300);
        rightOffsetAnimation.InsertKeyFrame(0f, new Vector3(-50f, 0f, 0f), new CubicEaseOut());
        rightOffsetAnimation.InsertKeyFrame(1f, new Vector3(0f, 0f, 0f), new CubicEaseOut());
        rightOffsetAnimation.Target = "Offset";

        var rightOpacityAnimation = _compositor.CreateScalarKeyFrameAnimation();
        rightOpacityAnimation.Duration = TimeSpan.FromMilliseconds(300);
        rightOpacityAnimation.InsertKeyFrame(0f, 0f, new CubicEaseOut());
        rightOpacityAnimation.InsertKeyFrame(1f, 1f, new CubicEaseOut());
        rightOpacityAnimation.Target = "Opacity";

        RunNavigationAnimation(leftOffsetAnimation, leftOpacityAnimation, rightOffsetAnimation, rightOpacityAnimation);
    }

    private void RunNavigationAnimation(
        CompositionAnimation leftOffset,
        CompositionAnimation leftOpacity,
        CompositionAnimation rightOffset,
        CompositionAnimation rightOpacity)
    {
        if (_compositor == null || _leftNavigationControlVisual == null || _rightNavigationControlVisual == null)
            return;

        var leftAnimationGroup = _compositor.CreateAnimationGroup();
        leftAnimationGroup.Add(leftOffset);
        leftAnimationGroup.Add(leftOpacity);

        var rightAnimationGroup = _compositor.CreateAnimationGroup();
        rightAnimationGroup.Add(rightOffset);
        rightAnimationGroup.Add(rightOpacity);

        _leftNavigationControlVisual.StartAnimationGroup(leftAnimationGroup);
        _rightNavigationControlVisual.StartAnimationGroup(rightAnimationGroup);
    }

    #endregion
}

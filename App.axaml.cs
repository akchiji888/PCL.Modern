using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using PCL.Neo_InML.Helpers;

namespace PCL.Neo_InML;

public partial class App : Application
{
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        // 注意：这里先不调用 ThemeHelper.Attach。
        // ThemeHelper 用的是 PCL.Neo 原版的 _colorHue = 50，会把 Colors.axaml 里的
        // PCL 蓝整体重算成金色。等重写 MainWindow 时再由 Controller 决定是否启用。
        // 需要启用时把下面一行取消注释即可：
        // ThemeHelper.Attach(this);

        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.MainWindow = new MainWindow();
        }

        base.OnFrameworkInitializationCompleted();
    }
}

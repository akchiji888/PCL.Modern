using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Metadata;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Shapes;
using Avalonia.Media;
using PCL.Neo_InML.Helpers;
using PCL.Neo_InML.Models;
using PCL.Neo_InML.Utils;
using System;

namespace PCL.Neo_InML.Controls;

[PseudoClasses(":white", ":highlight")]
public class MyRadioButton : RadioButton
{
    private Path? _shapeLogo;
    private TextBlock? _labText;

    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        base.OnApplyTemplate(e);
        _shapeLogo = e.NameScope.Find<Path>("ShapeLogo")!;
        _labText = e.NameScope.Find<TextBlock>("LabText")!;

        _shapeLogo.Data = Logo;
        _shapeLogo.RenderTransform = new ScaleTransform { ScaleX = LogoScale, ScaleY = LogoScale };
        _labText.Text = Text;

        SetPseudoClass();
    }

    public int Uuid = CoreUtils.GetUuid();

    public static readonly StyledProperty<Geometry> LogoProperty = AvaloniaProperty.Register<MyRadioButton, Geometry>(
        nameof(Logo));

    public Geometry Logo
    {
        get => GetValue(LogoProperty);
        set
        {
            SetValue(LogoProperty, value);
            if (_shapeLogo != null)
            {
                _shapeLogo.Data = value;
            }
        }
    }

    public static readonly StyledProperty<double> LogoScaleProperty = AvaloniaProperty.Register<MyRadioButton, double>(
        nameof(LogoScale),
        1);

    public double LogoScale
    {
        get => GetValue(LogoScaleProperty);
        set
        {
            SetValue(LogoScaleProperty, value);
            if (_shapeLogo != null)
            {
                _shapeLogo.RenderTransform = new ScaleTransform { ScaleX = value, ScaleY = value };
            }
        }
    }

    public static readonly StyledProperty<string> TextProperty = AvaloniaProperty.Register<MyRadioButton, string>(
        nameof(Text),
        string.Empty);

    public string Text
    {
        get => GetValue(TextProperty);
        set
        {
            SetValue(TextProperty, value);
            if (_labText != null)
            {
                _labText.Text = value;
            }
        }
    }

    public enum ColorState
    {
        White,
        HighLight
    }

    public static readonly StyledProperty<ColorState> ColorTypeProperty =
        AvaloniaProperty.Register<MyRadioButton, ColorState>(nameof(ColorType));

    public ColorState ColorType
    {
        get => GetValue(ColorTypeProperty);
        set
        {
            SetValue(ColorTypeProperty, value);
            SetPseudoClass();
        }
    }

    [Obsolete]
    private void SetCheck()
    {
        if (Parent is Panel parent)
        {
            foreach (var child in parent.Children)
            {
                if (child is MyRadioButton radioButton && radioButton != this)
                {
                    radioButton.IsChecked = false;
                }
            }
        }
    }

    private void SetPseudoClass()
    {
        switch (ColorType)
        {
            case ColorState.White:
                PseudoClasses.Set(":white", true);
                break;
            case ColorState.HighLight:
                PseudoClasses.Set(":highlight", true);
                break;
        }
    }
}

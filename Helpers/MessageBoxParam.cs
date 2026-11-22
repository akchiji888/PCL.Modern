using System;

namespace PCL.Neo_InML.Helpers;

public class MessageBoxParam
{
    public string? Message;
    public string Title = "提示";
    public string Button1Text = "确定";
    public string? Button2Text;
    public string? Button3Text;
    public Action<int>? ButtonPressedAction;
    public MessageBoxType Type = MessageBoxType.Notification;
    public bool IsCloseWhenButton1Pressed;
    public bool IsCloseWhenButton2Pressed;
    public bool IsCloseWhenButton3Pressed;
}

public enum MessageBoxType
{
    Notification,
    Warning,
    Error
}

public class MessageBoxResult
{
    public bool Button1Clicked;
    public bool Button2Clicked;
    public bool Button3Clicked;
    public bool Button1ClickedBeforeClose;
    public bool Button2ClickedBeforeClose;
    public bool Button3ClickedBeforeClose;
}

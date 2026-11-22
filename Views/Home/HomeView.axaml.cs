using Avalonia;
using Avalonia.Controls;
using Avalonia.Input.Platform;
using Avalonia.Interactivity;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Avalonia.Threading;
using MinecraftLaunch.Base.Models.Authentication;
using MinecraftLaunch.Base.Models.Authentication.Microsoft;
using MinecraftLaunch.Components.Authenticator;
using MinecraftLaunch.Components.Provider;
using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using PCL.Neo_InML;

namespace PCL.Neo_InML.Views.Home;

public partial class HomeView : UserControl
{
    // ============================================================
    // TODO: 在这里填入 Azure 应用的客户端 ID，为空时点「登录」会提示。
    // 另外按文档要求，Mojang 那边的申请表单需要已通过，否则 Minecraft 那一步会失败。
    // ============================================================
    private string ClientId = Keys.ClientId;

    /// <summary>
    /// 最近一次登录成功的微软账户。
    /// </summary>
    public MicrosoftAccount? CurrentAccount { get; private set; }

    /// <summary>
    /// 当前玩家皮肤位图（持有引用，防止 CroppedBitmap 的源被回收）。
    /// </summary>
    private Bitmap? PlayerSkin { get; set; }

    public HomeView()
    {
        InitializeComponent();
        LoadDefaultAvatar();
    }

    private void OnLoginModeClick(object? sender, RoutedEventArgs e)
    {
        if (sender is Control { Tag: string mode })
        {
            ShowLoginMode(mode);
        }
    }

    /// <summary>
    /// 切换正版 / 离线登录界面。
    /// </summary>
    /// <param name="mode">"premium" 或 "offline"。</param>
    public void ShowLoginMode(string mode)
    {
        var isPremium = mode == "premium";

        PanPremium.IsVisible = isPremium;
        PanOffline.IsVisible = !isPremium;
        PanLoggedIn.IsVisible = false;

        BtnLoginPremium.IsChecked = isPremium;
        BtnLoginOffline.IsChecked = !isPremium;
    }

    /// <summary>
    /// 登录成功后切到「玩家头像 + 玩家名」的界面。
    /// </summary>
    /// <param name="playerName">玩家名。</param>
    public void ShowLoggedIn(string playerName)
    {
        PanPremium.IsVisible = false;
        PanOffline.IsVisible = false;
        PanLoggedIn.IsVisible = true;

        ShowStatus(string.Empty);
        TxtPlayerName.Text = playerName;
    }

    /// <summary>
    /// 微软正版登录：设备代码流，浏览器里完成授权。
    /// </summary>
    private async void BtnPremiumLogin_Click(object? sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(ClientId))
        {
            ShowStatus("还没配置 Azure 客户端 ID，请在 HomeView.axaml.cs 里填写 ClientId。");
            return;
        }

        BtnPremiumLogin.IsEnabled = false;

        try
        {
            var authenticator = new MicrosoftAuthenticator(ClientId);

            // DeviceFlowAuthAsync 内部会一直轮询，直到用户在浏览器里完成登录或超时；
            // 回调里给出一次性代码和验证地址。
            var token = await authenticator.DeviceFlowAuthAsync(deviceCode =>
            {
                // 回调在后台线程，切回 UI 线程再更新界面 / 操作剪贴板
                Dispatcher.UIThread.Post(() => _ = HandleDeviceCodeAsync(deviceCode));
            }, CancellationToken.None);

            ShowStatus("正在验证 Xbox Live 与 Minecraft 账户…");

            var account = await authenticator.AuthenticateAsync(token, CancellationToken.None);
            CurrentAccount = account;

            // 先按 Steve 占位把界面切过去，皮肤拉回来后再替换
            ShowLoggedIn(account.Name);
            await LoadPlayerSkinAsync(account);
        }
        catch (Exception ex)
        {
            ShowStatus($"登录失败：{ex.Message}");
        }
        finally
        {
            BtnPremiumLogin.IsEnabled = true;
        }
    }

    /// <summary>
    /// 拿到一次性代码后：复制到剪贴板、提示用户、打开浏览器。
    /// </summary>
    private async Task HandleDeviceCodeAsync(DeviceCodeResponse deviceCode)
    {
        var copied = await CopyToClipboardAsync(deviceCode.UserCode);

        ShowStatus(copied
            ? $"代码 {deviceCode.UserCode} 已复制，请在浏览器中粘贴"
            : $"请在浏览器中输入代码：{deviceCode.UserCode}");

        OpenBrowser(deviceCode.VerificationUrl);
    }

    /// <summary>
    /// 把文本写入系统剪贴板。
    /// </summary>
    /// <returns>写入成功返回 true；剪贴板不可用时返回 false。</returns>
    private async Task<bool> CopyToClipboardAsync(string text)
    {
        try
        {
            var clipboard = TopLevel.GetTopLevel(this)?.Clipboard;

            if (clipboard is null)
                return false;

            await clipboard.SetTextAsync(text);
            return true;
        }
        catch (Exception)
        {
            // 剪贴板被占用/不可用时不打断登录流程，状态栏里仍然会显示代码
            return false;
        }
    }

    /// <summary>
    /// 用系统默认浏览器打开授权页（仅 Windows）。
    /// </summary>
    private static void OpenBrowser(string url)
    {
        if (!OperatingSystem.IsWindows())
            return;

        Process.Start(new ProcessStartInfo { FileName = url, UseShellExecute = true });
    }

    private void ShowStatus(string text)
    {
        TxtAuthStatus.Text = text;
        TxtAuthStatus.IsVisible = !string.IsNullOrEmpty(text);
    }

    /// <summary>
    /// 皮肤贴图是 64x64 的全身图，头部位于 (8,8) 的 8x8 区域，裁出来再做像素放大。
    /// </summary>
    private void LoadDefaultAvatar()
    {
        var skin = new Bitmap(AssetLoader.Open(new Uri("avares://PCL.Neo_InML/Assets/Skins/Steve.png")));
        var head = new CroppedBitmap(skin, new PixelRect(8, 8, 8, 8));

        ImgOfflineAvatar.Source = head;
        ImgPlayerAvatar.Source = head;
    }

    /// <summary>
    /// 从微软账户拉取皮肤，并把头部贴到头像上。
    /// 失败时保持 Steve 占位，不影响登录结果。
    /// </summary>
    private async Task LoadPlayerSkinAsync(MicrosoftAccount account)
    {
        try
        {
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(15));

            await using var skinStream = await SkinProvider.GetMicrosoftSkinDataAsync(account, cts.Token);

            // 转到内存流，保证 Bitmap 拿到的是可定位的流
            using var memory = new MemoryStream();
            await skinStream.CopyToAsync(memory, cts.Token);
            memory.Position = 0;

            var skinBitmap = new Bitmap(memory);

            // 持有源位图，避免 CroppedBitmap 用到的底层数据被回收
            PlayerSkin = skinBitmap;

            ImgPlayerAvatar.Source = new CroppedBitmap(skinBitmap, new PixelRect(8, 8, 8, 8));
        }
        catch (Exception)
        {
            // 皮肤拉取失败：继续用 Steve 占位
        }
    }
}

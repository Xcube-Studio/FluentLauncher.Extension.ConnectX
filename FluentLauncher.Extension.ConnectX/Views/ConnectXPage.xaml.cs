using ConnectX.Shared.Messages.Group;
using FluentLauncher.Extension.ConnectX.ViewModels;
using FluentLauncher.Infra.ExtensionHost;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;

namespace FluentLauncher.Extension.ConnectX.Views;

public sealed partial class ConnectXPage : Page
{
    ConnectXViewModel VM => (ConnectXViewModel)DataContext;

    public ConnectXPage()
    {
        this.LoadComponent(ref _contentLoaded);
    }

    private void Page_Loaded(object sender, RoutedEventArgs e)
    {
        VM.Dispatcher = this.DispatcherQueue;
        VM.IsActive = true;

        //VM.CheckAccount(XamlRoot);
    }

    private void Page_Unloaded(object sender, RoutedEventArgs e)
    {
        VM.IsActive = false;
    }

    private void Border_PointerEntered(object sender, PointerRoutedEventArgs e)
    {
        Border border = (Border)sender;

        if (VM.IsRoomOwner && border.FindName("KickButton") is HyperlinkButton button)
        {
            if (button.CommandParameter is UserInfo user && user.UserId != VM.RoomInfo?.RoomOwnerId)
            {
                button.Visibility = Visibility.Visible;
            }
        }
    }

    private void Border_PointerExited(object sender, PointerRoutedEventArgs e)
    {
        Border border = (Border)sender;

        if (border.FindName("KickButton") is HyperlinkButton button)
            button.Visibility = Visibility.Collapsed;
    }

    private void KickButton_Loaded(object sender, RoutedEventArgs e)
    {
        HyperlinkButton button = (HyperlinkButton)sender;
        button.Command = VM.KickUserCommand;
    }

    internal static string GetNameFromDisplayName(string displayName)
    {
        string type = displayName[..2];

        return type switch
        {
            "M:" => displayName[2..],
            "Y:" => displayName[2..],
            "O:" => displayName[2..],
            _ => displayName
        };
    }

    internal static string GetTypeFromDisplayName(string displayName)
    {
        string type = displayName[..2];

        return type switch
        {
            "M:" => "微软",
            "Y:" => "外置",
            "O:" => "离线",
            _ => "未知"
        };
    }
}

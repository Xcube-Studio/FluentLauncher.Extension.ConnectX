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

        if (VM.IsRoomOwner && border.FindName("KickButton") is Button button)
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

        if (border.FindName("KickButton") is Button button)
            button.Visibility = Visibility.Collapsed;
    }

    private void KickButton_Loaded(object sender, RoutedEventArgs e)
    {
        Button button = (Button)sender;
        button.Command = VM.KickUserCommand;
    }
}

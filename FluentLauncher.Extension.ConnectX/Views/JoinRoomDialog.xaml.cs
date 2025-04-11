using FluentLauncher.Extension.ConnectX.ViewModels;
using FluentLauncher.Infra.ExtensionHost;
using Microsoft.UI.Xaml.Controls;

namespace FluentLauncher.Extension.ConnectX.Views;

public sealed partial class JoinRoomDialog : ContentDialog
{
    JoinRoomDialogViewModel VM => (JoinRoomDialogViewModel)DataContext;

    public JoinRoomDialog()
    {
        this.LoadComponent(ref _contentLoaded);
    }
}

using FluentLauncher.Extension.ConnectX.ViewModels;
using FluentLauncher.Infra.ExtensionHost;
using Microsoft.UI.Xaml.Controls;

namespace FluentLauncher.Extension.ConnectX.Views;

public sealed partial class CreateRoomDialog : ContentDialog
{
    CreateRoomDialogViewModel VM => (CreateRoomDialogViewModel)DataContext;

    public CreateRoomDialog()
    {
        this.LoadComponent(ref _contentLoaded);
    }
}

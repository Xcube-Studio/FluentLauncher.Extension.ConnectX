using FluentLauncher.Extension.ConnectX.ViewModels;
using FluentLauncher.Infra.ExtensionHost;
using Microsoft.UI.Xaml.Controls;

namespace FluentLauncher.Extension.ConnectX.Views;

public sealed partial class RequestRedirectDialog : ContentDialog
{
    RequestRedirectDialogViewModel VM => (RequestRedirectDialogViewModel)DataContext;

    public RequestRedirectDialog()
    {
        this.LoadComponent(ref _contentLoaded);
    }
}
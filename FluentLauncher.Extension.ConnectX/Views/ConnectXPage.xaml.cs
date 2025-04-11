using FluentLauncher.Extension.ConnectX.ViewModels;
using FluentLauncher.Infra.ExtensionHost;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

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
    }

    private void Page_Unloaded(object sender, RoutedEventArgs e)
    {
        VM.IsActive = false;
    }
}

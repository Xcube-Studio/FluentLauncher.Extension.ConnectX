using CommunityToolkit.Mvvm.ComponentModel;
using ConnectX.Shared.Messages.Server;
using FluentLauncher.Infra.UI.Dialogs;

namespace FluentLauncher.Extension.ConnectX.ViewModels;

internal partial class RequestRedirectDialogViewModel : ObservableRecipient, IDialogParameterAware
{
    [ObservableProperty]
    public partial InterconnectServerRegistration InterconnectServer { get; set; }

    void IDialogParameterAware.HandleParameter(object param) => InterconnectServer = (InterconnectServerRegistration)param;
}
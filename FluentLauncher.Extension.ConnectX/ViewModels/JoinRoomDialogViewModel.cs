using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ConnectX.Shared.Messages.Group;
using FluentLauncher.Extension.ConnectX.Services;
using FluentLauncher.Infra.UI.Dialogs;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Threading.Tasks;

namespace FluentLauncher.Extension.ConnectX.ViewModels;

internal partial class JoinRoomDialogViewModel(RoomService roomService) : ObservableRecipient, IDialogParameterAware
{
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanJoinRoom))]
    public partial string RoomShortId { get; set; } = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanJoinRoom))]
    public partial string RoomPassword { get; set; } = string.Empty;
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanJoinRoom))]

    public partial bool IsPrivate { get; set; }

    private IDialogActivationService<ContentDialogResult> DialogActivationService { get; set; } = null!;

    public bool CanJoinRoom
    {
        get
        {
            if (string.IsNullOrEmpty(RoomShortId) || string.IsNullOrWhiteSpace(RoomShortId))
                return false;

            if (IsPrivate && (string.IsNullOrEmpty(RoomPassword) || string.IsNullOrWhiteSpace(RoomPassword)))
                return false;

            return true;
        }
    }

    void IDialogParameterAware.HandleParameter(object param) => DialogActivationService = (IDialogActivationService<ContentDialogResult>)param;

    [RelayCommand]
    async Task JoinRoomAsync()
    {
        await roomService.JoinRoomAsync(new JoinGroup()
        {
            GroupId = Guid.Empty,
            RoomShortId = RoomShortId,
            RoomPassword = RoomPassword
        }, async s => await DialogActivationService.ShowAsync("ConnectXRequestRedirectDialog", s) == ContentDialogResult.Primary);
    }
}
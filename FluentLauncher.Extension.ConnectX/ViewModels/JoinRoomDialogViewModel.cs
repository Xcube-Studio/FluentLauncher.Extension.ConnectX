using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FluentLauncher.Extension.ConnectX.Services;
using System;
using System.Threading.Tasks;

namespace FluentLauncher.Extension.ConnectX.ViewModels;

internal partial class JoinRoomDialogViewModel(RoomService roomService) : ObservableRecipient
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

    [ObservableProperty]
    public partial bool UseRelay { get; set; }

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

    [RelayCommand]
    async Task JoinRoomAsync()
    {
        await roomService.JoinRoomAsync(new()
        {
            GroupId = Guid.Empty,
            RoomShortId = RoomShortId,
            RoomPassword = RoomPassword,
            UseRelayServer = UseRelay
        });
    }
}
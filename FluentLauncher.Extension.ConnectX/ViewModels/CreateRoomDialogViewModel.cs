using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ConnectX.Client.Interfaces;
using ConnectX.Shared.Messages.Group;
using FluentLauncher.Extension.ConnectX.Services;
using System.Threading.Tasks;

namespace FluentLauncher.Extension.ConnectX.ViewModels;

internal partial class CreateRoomDialogViewModel(
    IServerLinkHolder serverLinkHolder, 
    RoomService roomService) : ObservableRecipient
{
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanCreateRoom))]
    public partial string RoomName { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string RoomDescription { get; set; } = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanCreateRoom))]
    public partial string RoomPassword { get; set; } = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanCreateRoom))]
    public partial bool IsPrivate { get; set; }

    [ObservableProperty]
    public partial bool UseRelay { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanCreateRoom))]
    public partial int MaxUserNumber { get; set; } = 3;

    public bool CanCreateRoom
    {
        get
        {
            if (string.IsNullOrEmpty(RoomName) || string.IsNullOrWhiteSpace(RoomName)) 
                return false;

            if (MaxUserNumber < 2 && MaxUserNumber > 10) 
                return false;

            if (IsPrivate && (string.IsNullOrEmpty(RoomPassword) || string.IsNullOrWhiteSpace(RoomPassword))) 
                return false;

            return true;
        }
    }

    [RelayCommand]
    async Task CreateRoom()
    {
        await roomService.CreateRoomAsync(new CreateGroup
        {
            MaxUserCount = MaxUserNumber,
            RoomName = RoomName,
            RoomDescription = RoomDescription,
            RoomPassword = RoomPassword,
            IsPrivate = IsPrivate,
            UseRelayServer = UseRelay,
            UserId = serverLinkHolder.UserId
        });
    }
}

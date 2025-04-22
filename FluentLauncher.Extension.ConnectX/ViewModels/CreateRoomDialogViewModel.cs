using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ConnectX.Shared.Messages.Group;
using FluentLauncher.Extension.ConnectX.Services;
using System;
using System.Threading.Tasks;

namespace FluentLauncher.Extension.ConnectX.ViewModels;

internal partial class CreateRoomDialogViewModel : ObservableRecipient
{
    private readonly RoomService _roomService;
    private readonly AccountService _accountService;

    public CreateRoomDialogViewModel(RoomService roomService, AccountService accountService)
    {
        _accountService = accountService;
        _roomService = roomService;

        object account = accountService.ActiveAccount;
        Type accountType = account.GetType();

        RoomName = $"{accountType.GetProperty("Name")?.GetValue(account) as string ?? "用户 Unknown"} 的房间";
    }

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
        await _roomService.CreateRoomAsync(new CreateGroup
        {
            MaxUserCount = MaxUserNumber,
            RoomName = RoomName,
            RoomDescription = RoomDescription,
            RoomPassword = RoomPassword,
            IsPrivate = IsPrivate,
            UseRelayServer = UseRelay
        });
    }
}

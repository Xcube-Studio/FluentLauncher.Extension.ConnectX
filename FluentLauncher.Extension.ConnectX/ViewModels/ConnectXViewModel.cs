using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using CommunityToolkit.WinUI;
using ConnectX.Client;
using ConnectX.Client.Interfaces;
using ConnectX.Shared.Messages.Group;
using FluentLauncher.Extension.ConnectX.Messages;
using FluentLauncher.Extension.ConnectX.Model;
using FluentLauncher.Extension.ConnectX.Services;
using FluentLauncher.Infra.UI.Dialogs;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System.Threading.Tasks;
using Windows.ApplicationModel.DataTransfer;

using ConnectXClient = ConnectX.Client.Client;

namespace FluentLauncher.Extension.ConnectX.ViewModels;

internal partial class ConnectXViewModel(
    RoomService roomService,
    ConnectService connectService,
    ConnectXClient client,
    ClientSettingProvider settingProvider,
    IServerLinkHolder serverLinkHolder,
    IDialogActivationService<ContentDialogResult> dialogService) : ObservableRecipient,
    IRecipient<RoomStateChangedMessage>,
    IRecipient<ServerConnectFailedMessage>, 
    IRecipient<ServerStateChangedMessage>,
    IRecipient<RoomInfoUpdatedMessage>
{
    internal DispatcherQueue Dispatcher { get; set; } = null!;

    [ObservableProperty]
    public partial bool ConnectFailed { get; set; } = connectService.ConnectFailed;

    [ObservableProperty]
    public partial string Ping { get; set; } = "-";

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsConnected))]
    [NotifyPropertyChangedFor(nameof(IsConnecting))]
    [NotifyPropertyChangedFor(nameof(ConnectButtonEnabled))]
    [NotifyPropertyChangedFor(nameof(CanCreateOrJoinRoom))]
    public partial ServerConnectStatus ConnectStatus { get; set; } = connectService.Status;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanCreateOrJoinRoom))]
    public partial bool IsInRoom { get; set; } = roomService.IsInRoom;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsRoomOwner))]
    [NotifyPropertyChangedFor(nameof(ShowRoomDescription))]
    [NotifyPropertyChangedFor(nameof(PingButtonVisibility))]
    public partial GroupInfo? RoomInfo { get; set; } = roomService.GroupInfo;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanCreateOrJoinRoom))]
    public partial bool OperatingRoom { get; set; }

    [ObservableProperty]
    public partial bool CopyTeachingTipIsOpen { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ShowUserServerAddressBox))]
    public partial int ServerNodeSelection { get; set; } = settingProvider.ServerNodeSelection;

    [ObservableProperty]
    public partial string UserServerAddress { get; set; } = settingProvider.UserServerAddress;

    public bool ConnectButtonEnabled => ConnectStatus == ServerConnectStatus.Disconnected;

    public bool CanCreateOrJoinRoom => IsConnected && !IsInRoom && !OperatingRoom;

    public bool IsRoomOwner => RoomInfo?.RoomOwnerId == serverLinkHolder.UserId;

    public bool ShowRoomDescription => !string.IsNullOrWhiteSpace(RoomInfo?.RoomDescription);

    public bool IsConnected => ConnectStatus == ServerConnectStatus.Connected;

    public bool IsConnecting => ConnectStatus == ServerConnectStatus.Connecting;

    public bool ShowUserServerAddressBox => ServerNodeSelection == 2;

    public Visibility PingButtonVisibility => IsInRoom && !IsRoomOwner ? Visibility.Visible : Visibility.Collapsed;


    //partial void OnIsInRoomChanged(bool value)
    //{
    //    if (value && !IsRoomOwner)
    //    {
    //        Task.Run(async () =>
    //        {
    //            var (connectable, isDirect, ping) = client.GetPartnerConState(roomService.GroupInfo!.RoomOwnerId);
    //            await Dispatcher.EnqueueAsync(() => Ping = ping.ToString());
    //        });
    //    }
    //}

    partial void OnServerNodeSelectionChanged(int value) => settingProvider.ServerNodeSelection = value;

    partial void OnUserServerAddressChanged(string value) => settingProvider.UserServerAddress = value;

    [RelayCommand]
    async Task TryConnectService() => await connectService.ConnectAsync();

    [RelayCommand]
    async Task DisconnectService() => await connectService.DisconnectAsync();

    [RelayCommand]
    async Task CreateRoom()
    {
        OperatingRoom = true;
        TaskCompletionSource tcs = new();

        await dialogService.ShowAsync("ConnectXCreateRoomDialog", tcs);
        await tcs.Task;

        OperatingRoom = false;
    }

    [RelayCommand]
    async Task JoinRoom()
    {
        OperatingRoom = true;
        TaskCompletionSource tcs = new();

        await dialogService.ShowAsync("ConnectXJoinRoomDialog", tcs);
        await tcs.Task;

        OperatingRoom = false;
    }

    [RelayCommand]
    async Task LeaveRoom()
    {
        OperatingRoom = true;

        await roomService.LeaveRoom();

        OperatingRoom = false;
    }

    [RelayCommand]
    void CopyShortId()
    {
        if (RoomInfo is not null)
        {
            DataPackage dataPackage = new();
            dataPackage.SetText(RoomInfo.RoomShortId);

            Clipboard.SetContent(dataPackage);
            CopyTeachingTipIsOpen = true;
        }
    }

    [RelayCommand]
    async Task RefreshPing()
    {
        PartnerConnectionState? connectionState = null;

        await Task.Run(() => connectionState = client.GetPartnerConState(roomService.GroupInfo!.RoomOwnerId));
        await Dispatcher.EnqueueAsync(() => Ping = connectionState?.Latency.ToString() ?? "-");
    }

    async void IRecipient<RoomStateChangedMessage>.Receive(RoomStateChangedMessage message)
        => await Dispatcher.EnqueueAsync(() => IsInRoom = message.Value);

    async void IRecipient<ServerConnectFailedMessage>.Receive(ServerConnectFailedMessage message)
        => await Dispatcher.EnqueueAsync(() => ConnectFailed = true);

    async void IRecipient<RoomInfoUpdatedMessage>.Receive(RoomInfoUpdatedMessage _)
        => await Dispatcher.EnqueueAsync(() => RoomInfo = roomService.GroupInfo);

    async void IRecipient<ServerStateChangedMessage>.Receive(ServerStateChangedMessage message)
    {
        await Dispatcher.EnqueueAsync(() =>
        {
            ConnectStatus = message.Value;

            if (message.Value == ServerConnectStatus.Connected)
                ConnectFailed = false;
        });
    }
}

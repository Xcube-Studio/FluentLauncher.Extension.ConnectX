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
using FluentLauncher.Infra.UI.Navigation;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Threading.Tasks;
using Windows.ApplicationModel.DataTransfer;

using ConnectXClient = ConnectX.Client.Client;

namespace FluentLauncher.Extension.ConnectX.ViewModels;

internal partial class ConnectXViewModel(
    AccountService accountService,
    RoomService roomService,
    ConnectService connectService,
    ConnectXClient client,
    ClientSettingProvider settingProvider,
    IServerLinkHolder serverLinkHolder,
    INavigationService navigationService,
    IDialogActivationService<ContentDialogResult> dialogService) : ObservableRecipient,
    IRecipient<ServerConnectFailedMessage>, 
    IRecipient<ServerConnectStatusChangedMessage>,
    IRecipient<RoomOperatingMessage>,
    IRecipient<RoomInfoUpdatedMessage>,
    IRecipient<RoomStateChangedMessage>,
    INavigationAware
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
    public partial bool IsOperatingRoom { get; set; } = roomService.IsOperatingRoom;

    [ObservableProperty]
    public partial bool CopyTeachingTipIsOpen { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ShowUserServerAddressBox))]
    public partial int ServerNodeSelection { get; set; } = settingProvider.ServerNodeSelection;

    [ObservableProperty]
    public partial string UserServerAddress { get; set; } = settingProvider.UserServerAddress;

    public bool ConnectButtonEnabled => ConnectStatus == ServerConnectStatus.Disconnected;

    public bool CanCreateOrJoinRoom => IsConnected && !IsInRoom && !IsOperatingRoom;

    public bool IsRoomOwner => RoomInfo?.RoomOwnerId == serverLinkHolder.UserId;

    public bool ShowRoomDescription => !string.IsNullOrWhiteSpace(RoomInfo?.RoomDescription);

    public bool IsConnected => ConnectStatus == ServerConnectStatus.Connected;

    public bool IsConnecting => ConnectStatus == ServerConnectStatus.Connecting;

    public bool ShowUserServerAddressBox => ServerNodeSelection == 2;

    public Visibility PingButtonVisibility => IsInRoom && !IsRoomOwner ? Visibility.Visible : Visibility.Collapsed;

    partial void OnServerNodeSelectionChanged(int value) => settingProvider.ServerNodeSelection = value;

    partial void OnUserServerAddressChanged(string value) => settingProvider.UserServerAddress = value;

    [RelayCommand]
    async Task TryConnectService() => await connectService.ConnectAsync();

    [RelayCommand]
    async Task DisconnectService() => await connectService.DisconnectAsync();

    [RelayCommand]
    async Task CreateRoom() => await dialogService.ShowAsync("ConnectXCreateRoomDialog");

    [RelayCommand]
    async Task JoinRoom() => await dialogService.ShowAsync("ConnectXJoinRoomDialog");

    [RelayCommand]
    async Task LeaveRoom() => await roomService.LeaveRoom();

    [RelayCommand]
    async Task KickUser(UserInfo user) => await roomService.KickUserAsync(user.UserId);

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

    async void IRecipient<RoomOperatingMessage>.Receive(RoomOperatingMessage message)
        => await Dispatcher.EnqueueAsync(() => IsOperatingRoom = message.Value);

    async void IRecipient<ServerConnectStatusChangedMessage>.Receive(ServerConnectStatusChangedMessage message)
    {
        await Dispatcher.EnqueueAsync(() =>
        {
            ConnectStatus = message.Value;

            if (message.Value == ServerConnectStatus.Connected)
                ConnectFailed = false;
        });
    }

    //internal async void CheckAccount(XamlRoot xamlRoot)
    //{
    //    if (!accountService.HasMicrosoftAccount())
    //    {
    //        await new ContentDialog()
    //        {
    //            XamlRoot = xamlRoot,
    //            Title = "需要正版验证",
    //            Content = new TextBlock()
    //            {
    //                Text = "你至少需要在启动器中登录一个正版账号才能使用此功能"
    //            },
    //            DefaultButton = ContentDialogButton.Close,
    //            CloseButtonText = "确定"
    //        }.ShowAsync();

    //        navigationService.GoBack();
    //    }
    //}
}

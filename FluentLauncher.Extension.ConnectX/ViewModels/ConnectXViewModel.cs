using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using CommunityToolkit.WinUI;
using ConnectX.Client;
using ConnectX.Client.Interfaces;
using ConnectX.Shared.Helpers;
using ConnectX.Shared.Messages.Group;
using ConnectX.Shared.Messages.Server;
using FluentLauncher.Extension.ConnectX.Messages;
using FluentLauncher.Extension.ConnectX.Model;
using FluentLauncher.Extension.ConnectX.Services;
using FluentLauncher.Infra.UI.Dialogs;
using FluentLauncher.Infra.UI.Navigation;
using FluentLauncher.Infra.UI.Notification;
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
    MultiCasterFinderService fakeMultiCasterService,
    ClientSettingProvider settingProvider,
    IServerLinkHolder serverLinkHolder,
    INotificationService notificationService,
    IDialogActivationService<ContentDialogResult> dialogService) : ObservableRecipient,
    IRecipient<ServerConnectFailedMessage>, 
    IRecipient<ServerConnectStatusChangedMessage>,
    IRecipient<RoomOperatingMessage>,
    IRecipient<RoomInfoUpdatedMessage>,
    IRecipient<RoomStateChangedMessage>,
    IRecipient<LanMultiCasterListenedMessage>,
    IRecipient<InterconnectStatusChangedMessage>,
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
    [NotifyPropertyChangedFor(nameof(ShowUserServerAddressBox))]
    public partial int ServerNodeSelection { get; set; } = settingProvider.ServerNodeSelection;

    [ObservableProperty]
    public partial string UserServerAddress { get; set; } = settingProvider.UserServerAddress;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(InterconnectedServerMotdVisibility))]
    public partial InterconnectServerRegistration? InterconnectServer {  get; set; } = settingProvider.InterconnectServer;

    [ObservableProperty]
    public partial bool ShowStabilityWarning { get; set; } = settingProvider.ShowStabilityWarning;

    public string ConnectXClientVersion { get; } = typeof(ConnectXClient).Assembly.GetName().Version?.ToString()!;

    public bool ConnectButtonEnabled => ConnectStatus == ServerConnectStatus.Disconnected;

    public bool CanCreateOrJoinRoom => IsConnected && !IsInRoom && !IsOperatingRoom;

    public bool IsRoomOwner => RoomInfo?.RoomOwnerId == serverLinkHolder.UserId;

    public bool ShowRoomDescription => !string.IsNullOrWhiteSpace(RoomInfo?.RoomDescription);

    public bool IsConnected => ConnectStatus == ServerConnectStatus.Connected;

    public bool IsConnecting => ConnectStatus == ServerConnectStatus.Connecting;

    public bool ShowUserServerAddressBox => ServerNodeSelection == 2;

    public Visibility PingButtonVisibility => IsInRoom && !IsRoomOwner ? Visibility.Visible : Visibility.Collapsed;

    public Visibility InterconnectedServerMotdVisibility => InterconnectServer != null ? Visibility.Visible : Visibility.Collapsed;

    public MultiCasterServerInfo? ListenedServerInfo => fakeMultiCasterService.ListenedServerInfo;

    public bool ListenedServer => fakeMultiCasterService.ListenedServer;

    partial void OnServerNodeSelectionChanged(int value) => settingProvider.ServerNodeSelection = value;

    partial void OnUserServerAddressChanged(string value) => settingProvider.UserServerAddress = value;

    partial void OnShowStabilityWarningChanged(bool value) => settingProvider.ShowStabilityWarning = value;

    partial void OnRoomInfoChanged(GroupInfo? value) => RefreshPing().Forget();

    [RelayCommand]
    async Task TryConnectService() => await connectService.ConnectAsync();

    [RelayCommand]
    async Task DisconnectService() => await connectService.DisconnectAsync();

    [RelayCommand]
    async Task CreateRoom() => await dialogService.ShowAsync("ConnectXCreateRoomDialog");

    [RelayCommand]
    async Task JoinRoom() => await dialogService.ShowAsync("ConnectXJoinRoomDialog", dialogService);

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
            notificationService.CopidShortId();
        }
    }

    [RelayCommand]
    async Task RefreshPing()
    {
        PartnerConnectionState? connectionState = null;

        await Task.Run(() => connectionState = client.GetPartnerConState(roomService.GroupInfo!.RoomOwnerId));
        await Dispatcher.EnqueueAsync(() => Ping = connectionState?.Latency.ToString() ?? "-");
    }

    [RelayCommand]
    void ShowClientVersion() => notificationService.ShowConnectXClientVersion(ConnectXClientVersion);

    async void IRecipient<RoomStateChangedMessage>.Receive(RoomStateChangedMessage message)
        => await Dispatcher.EnqueueAsync(() => IsInRoom = message.Value);

    async void IRecipient<ServerConnectFailedMessage>.Receive(ServerConnectFailedMessage message)
        => await Dispatcher.EnqueueAsync(() => ConnectFailed = true);

    async void IRecipient<RoomInfoUpdatedMessage>.Receive(RoomInfoUpdatedMessage _)
        => await Dispatcher.EnqueueAsync(() => RoomInfo = roomService.GroupInfo);

    async void IRecipient<RoomOperatingMessage>.Receive(RoomOperatingMessage message)
        => await Dispatcher.EnqueueAsync(() => IsOperatingRoom = message.Value);

    async void IRecipient<InterconnectStatusChangedMessage>.Receive(InterconnectStatusChangedMessage message)
        => await Dispatcher.EnqueueAsync(() => InterconnectServer = message.InterconnectServer);

    async void IRecipient<LanMultiCasterListenedMessage>.Receive(LanMultiCasterListenedMessage _)
    {
        await Dispatcher.EnqueueAsync(() =>
        {
            OnPropertyChanged(nameof(ListenedServer));
            OnPropertyChanged(nameof(ListenedServerInfo));
        });
    }

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

internal static partial class ConnectXViewModelNotifications
{
    public static void CopidShortId(this INotificationService notificationService)
    {
        notificationService.Show(new TeachingTipNotification
        {
            Title = "已复制到剪切板",
            Icon = "\ue73e"
        });
    }

    public static void ShowConnectXClientVersion(this INotificationService notificationService, string connectXClientVersion)
    {
        notificationService.Show(new TeachingTipNotification
        {
            Title = "ConnectX.Client 版本",
            Message = connectXClientVersion,
            CloseButtonContent = "确定",
            Icon = "\ue946"
        });
    }
}
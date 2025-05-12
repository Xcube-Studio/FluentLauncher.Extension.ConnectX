using CommunityToolkit.Mvvm.Messaging;
using ConnectX.Client.Interfaces;
using ConnectX.Shared.Messages.Server;
using FluentLauncher.Extension.ConnectX.Messages;
using FluentLauncher.Extension.ConnectX.Model;
using FluentLauncher.Infra.UI.Notification;
using System.Threading;
using System.Threading.Tasks;

namespace FluentLauncher.Extension.ConnectX.Services;

internal class ConnectService
{
    private readonly IServerLinkHolder _serverLinkHolder;
    private readonly ClientSettingProvider _clientSettingProvider;

    private readonly INotificationService _notificationService;

    public bool ConnectFailed 
    { 
        get => field; 
        private set 
        {
            field = value;

            if (field)
                WeakReferenceMessenger.Default.Send(new ServerConnectFailedMessage());
        }
    }

    public ServerConnectStatus Status
    {
        get => field;
        private set
        {
            if (field != value)
                WeakReferenceMessenger.Default.Send(new ServerConnectStatusChangedMessage(value));

            field = value;
        }
    } = ServerConnectStatus.Disconnected;

    public ConnectService(
        IServerLinkHolder serverLinkHolder, 
        ClientSettingProvider clientSettingProvider,
        INotificationService notificationService)
    {
        _serverLinkHolder = serverLinkHolder;
        _clientSettingProvider = clientSettingProvider;
        _notificationService = notificationService;

        _serverLinkHolder.OnServerLinkDisconnected += OnServerLinkDisconnected;
    }

    public async Task ConnectAsync()
    {
        Status = ServerConnectStatus.Connecting;

        try
        {
            await _serverLinkHolder.ConnectAsync(CancellationToken.None);
        }
        finally
        {
            Status = _serverLinkHolder.IsConnected
                ? ServerConnectStatus.Connected
                : ServerConnectStatus.Disconnected;

            if (!_serverLinkHolder.IsConnected)
                ConnectFailed = true;
        }
    }

    public async Task DisconnectAsync()
    {
        try
        {
            await _serverLinkHolder.DisconnectAsync(CancellationToken.None);
        }
        finally
        {
            Status = _serverLinkHolder.IsConnected
                ? ServerConnectStatus.Connected
                : ServerConnectStatus.Disconnected;
        }
    }

    public async Task RedirectAsync(InterconnectServerRegistration interconnectServer)
    {
        _clientSettingProvider.UseInterconnectServer(interconnectServer);

        await DisconnectAsync();
        await Task.Delay(1000);
        await ConnectAsync();

        _notificationService.Redirected(interconnectServer);
        WeakReferenceMessenger.Default.Send(new InterconnectStatusChangedMessage(true, interconnectServer));
    }

    public async Task RedirectToDefalutAsync()
    {
        _clientSettingProvider.UseSettingServer();

        await DisconnectAsync();
        await ConnectAsync();

        _notificationService.RedirectedToDefault();
        WeakReferenceMessenger.Default.Send(new InterconnectStatusChangedMessage(false, null));
    }

    private void OnServerLinkDisconnected() => Status = ServerConnectStatus.Disconnected;
}

internal static class ConnectServiceNotifications
{
    public static void Redirected(this INotificationService notificationService, InterconnectServerRegistration interconnectServer)
    {
        notificationService.Show(new Notification
        {
            Title = "已重定向连接到房间所在的服务节点",
            Message = $"服务节点名称：[{interconnectServer.ServerName}]\r\n{interconnectServer.ServerMotd}",
            Type = NotificationType.Success,
        });
    }

    public static void RedirectedToDefault(this INotificationService notificationService)
    {
        notificationService.Show(new Notification
        {
            Title = "已在重定向回原节点",
            Type = NotificationType.Success,
        });
    }
}
using CommunityToolkit.Mvvm.Messaging;
using ConnectX.Client.Interfaces;
using FluentLauncher.Extension.ConnectX.Messages;
using FluentLauncher.Extension.ConnectX.Model;
using System.Threading;
using System.Threading.Tasks;

namespace FluentLauncher.Extension.ConnectX.Services;

internal class ConnectService
{
    private readonly IServerLinkHolder _serverLinkHolder;

    public bool ConnectFailed 
    { 
        get => field; 
        private set 
        {
            field = value;

            if (value)
                WeakReferenceMessenger.Default.Send(new ServerConnectFailedMessage());
        }
    }

    public ServerConnectStatus Status
    {
        get => field;
        private set
        {
            field = value;
            WeakReferenceMessenger.Default.Send(new ServerConnectStatusChangedMessage(value));
        }
    } = ServerConnectStatus.Disconnected;

    public ConnectService(IServerLinkHolder serverLinkHolder)
    {
        _serverLinkHolder = serverLinkHolder;
        _serverLinkHolder.OnServerLinkDisconnected += OnServerLinkDisconnected;

        Task.Run(async () => await serverLinkHolder.StartAsync(default))
            .ContinueWith(task =>
            {
                Status = _serverLinkHolder.IsConnected
                    ? ServerConnectStatus.Connected
                    : ServerConnectStatus.Disconnected;

                if (!_serverLinkHolder.IsConnected)
                    ConnectFailed = true;
            });
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

    private void OnServerLinkDisconnected() => Status = ServerConnectStatus.Disconnected;
}

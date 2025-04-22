using CommunityToolkit.Mvvm.Messaging;
using ConnectX.Client.Interfaces;
using FluentLauncher.Extension.ConnectX.Messages;
using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using Timer = System.Timers.Timer;

namespace FluentLauncher.Extension.ConnectX.Services;

internal class FakeMultiCasterService
{
    private readonly IServerLinkHolder _serverLinkHolder;
    private readonly IRoomInfoManager _roomInfoManager;

    private DateTime? lastListenedTime;

    private const int MulticastPort = 4445;
    private static readonly IPAddress MulticastAddress = IPAddress.Parse("224.0.2.60");
    private static readonly IPEndPoint MulticastIpe = new(MulticastAddress, MulticastPort);

    public string? ListenedServerName { get; private set; } = null;

    public int? ListenedServerPort { get; private set; } = null;

    public bool ListenedServer { get; private set; } = false;

    public FakeMultiCasterService(IServerLinkHolder serverLinkHolder, IRoomInfoManager roomInfoManager)
    {
        _serverLinkHolder = serverLinkHolder;
        _roomInfoManager = roomInfoManager;

        Timer timer = new(TimeSpan.FromSeconds(2.5));
        timer.Elapsed += (_, _) =>
        {
            ListenedServer = lastListenedTime != null && DateTime.Now - lastListenedTime < TimeSpan.FromSeconds(5);

            if (!ListenedServer)
            {
                ListenedServerName = null;
                ListenedServerPort = null;
            }

            WeakReferenceMessenger.Default.Send(new LanMultiCasterListenedMessage());
        };

        Task.Run(async () => await ExecuteAsync(default));
        timer.Start();
    }

    private void OnListenedLanServer(string arg1, int arg2)
    {
        ListenedServerName = arg1;
        ListenedServerPort = arg2;

        lastListenedTime = DateTime.Now;
    }

    async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var buffer = new byte[256];

        while (!stoppingToken.IsCancellationRequested)
        {
            if (_roomInfoManager.CurrentGroupInfo == null)
            {
                await Task.Delay(2500, stoppingToken);
                continue;
            }

            using var multicastSocket = new Socket(SocketType.Dgram, ProtocolType.Udp);
            var multicastOption = new MulticastOption(MulticastAddress, IPAddress.Any);

            multicastSocket.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.AddMembership, multicastOption);
            multicastSocket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);

            multicastSocket.Bind(new IPEndPoint(IPAddress.Any, MulticastPort));

            var receiveFromResult = await multicastSocket.ReceiveFromAsync(buffer, MulticastIpe);
            var message = Encoding.UTF8.GetString(buffer, 0, receiveFromResult.ReceivedBytes);
            var serverName = message["[MOTD]".Length..message.IndexOf("[/MOTD]", StringComparison.Ordinal)];
            var portStart = message.IndexOf("[AD]", StringComparison.Ordinal) + 4;
            var portEnd = message.IndexOf("[/AD]", StringComparison.Ordinal);
            var port = ushort.Parse(message[portStart..portEnd]);

            if (!(_roomInfoManager.CurrentGroupInfo.RoomOwnerId == _serverLinkHolder.UserId && serverName.StartsWith("[ConnectX]")))
                OnListenedLanServer(serverName.TrimStart("[ConnectX]".ToCharArray()), port);

            multicastSocket.Close();

            await Task.Delay(3000, stoppingToken);
        }
    }

}

using CommunityToolkit.Mvvm.Messaging;
using ConnectX.Client.Messages.Proxy.MulticastMessages;
using ConnectX.Client.Proxy.FakeServerMultiCasters;
using ConnectX.Client.Route;
using ConnectX.Client.Transmission;
using FluentLauncher.Extension.ConnectX.Messages;
using FluentLauncher.Extension.ConnectX.Model;
using Microsoft.Extensions.Hosting;
using System;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace FluentLauncher.Extension.ConnectX.Services;

internal partial class MultiCasterFinderService : BackgroundService
{
    private DateTime? lastListenedTime;

    public bool ListenedServer { get; private set; } = false;

    public MultiCasterServerInfo? ListenedServerInfo { get; private set; }

    public MultiCasterFinderService(
        FakeServerMultiCasterV6 fakeServerMultiCasterV6,
        FakeServerMultiCasterV4 fakeServerMultiCasterV4,
        RouterPacketDispatcher routerPacketDispatcher,
        RelayPacketDispatcher relayPacketDispatcher)
    {
        fakeServerMultiCasterV4.OnListenedLanServer += (n, p) => OnListenedLanServer(AddressFamily.InterNetwork, n, p);
        fakeServerMultiCasterV6.OnListenedLanServer += (n, p) => OnListenedLanServer(AddressFamily.InterNetworkV6, n, p);

        routerPacketDispatcher.OnReceive<McMulticastMessageV4>((m, c) => OnListenedLanServer(AddressFamily.InterNetwork, m.Name, m.Port));
        routerPacketDispatcher.OnReceive<McMulticastMessageV6>((m, c) => OnListenedLanServer(AddressFamily.InterNetworkV6, m.Name, m.Port));

        relayPacketDispatcher.OnReceive<McMulticastMessageV4>((m, c) => OnListenedLanServer(AddressFamily.InterNetwork, m.Name, m.Port));
        relayPacketDispatcher.OnReceive<McMulticastMessageV6>((m, c) => OnListenedLanServer(AddressFamily.InterNetworkV6, m.Name, m.Port));

        WeakReferenceMessenger.Default.Register<RoomStateChangedMessage>(this, (r, m) =>
        {
            if (!m.Value)
            {
                ListenedServer = false;
                ListenedServerInfo = null;
                WeakReferenceMessenger.Default.Send(new LanMultiCasterListenedMessage());
            }
        });
    }

    private void OnListenedLanServer(AddressFamily addressFamily, string arg1, int arg2)
    {
        lastListenedTime = DateTime.Now;
        ListenedServerInfo = new(addressFamily, arg1, arg2);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            ListenedServer = lastListenedTime != null && DateTime.Now - lastListenedTime < TimeSpan.FromSeconds(5);

            if (!ListenedServer)
                ListenedServerInfo = null;

            WeakReferenceMessenger.Default.Send(new LanMultiCasterListenedMessage());

            await Task.Delay(2500, stoppingToken);
        }
    }
}

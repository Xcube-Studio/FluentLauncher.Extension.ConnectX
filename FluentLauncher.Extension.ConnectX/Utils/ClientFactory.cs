using ConnectX.Client;
using ConnectX.Client.Interfaces;
using ConnectX.Client.Managers;
using ConnectX.Client.Proxy.FakeServerMultiCasters;
using ConnectX.Client.Route;
using ConnectX.Client.Transmission;
using ConnectX.Shared.Helpers;
using Microsoft.Extensions.DependencyInjection;

namespace FluentLauncher.Extension.ConnectX.Utils;

public static class ClientFactory
{
    public static void UseConnectX(this IServiceCollection services)
    {
        services.AddSingleton<ClientSettingProvider>();
        services.AddSingleton<IClientSettingProvider>(p => p.GetRequiredService<ClientSettingProvider>());

        services.RegisterConnectXClientPackets();
        services.AddConnectXEssentials();

        services.AddSingleton<RelayPacketDispatcher>();

        // Router
        services.AddSingleton<RouterPacketDispatcher>();
        services.AddSingleton<RouteTable>();
        services.AddSingleton<Router>();
        services.AddHostedService(sp => sp.GetRequiredService<Router>());

        services.AddSingleton<IZeroTierNodeLinkHolder, ZeroTierNodeLinkHolder>();
        services.AddSingleton<IServerLinkHolder, ServerLinkHolder>();
        services.AddSingleton<IRoomInfoManager, RoomInfoManager>();

        services.AddHostedService(sp => sp.GetRequiredService<IZeroTierNodeLinkHolder>());
        services.AddHostedService(sp => sp.GetRequiredService<IServerLinkHolder>());
        services.AddHostedService(sp => sp.GetRequiredService<IRoomInfoManager>());

        services.AddSingleton<PeerManager>();
        services.AddHostedService(sp => sp.GetRequiredService<PeerManager>());

        services.AddSingleton<ProxyManager>();
        services.AddHostedService(sp => sp.GetRequiredService<ProxyManager>());

        services.AddSingleton<FakeServerMultiCasterV4>();
        services.AddSingleton<FakeServerMultiCasterV6>();

        services.AddHostedService(sp => sp.GetRequiredService<FakeServerMultiCasterV4>());
        services.AddHostedService(sp => sp.GetRequiredService<FakeServerMultiCasterV6>());

        services.AddSingleton<PartnerManager>();

        services.AddSingleton<Client>();
    }
}

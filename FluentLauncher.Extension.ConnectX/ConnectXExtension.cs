using ConnectX.Client.Helpers;
using ConnectX.Client.Interfaces;
using ConnectX.Client.Managers;
using ConnectX.Client.Proxy;
using ConnectX.Client.Route;
using FluentLauncher.Extension.ConnectX.Services;
using FluentLauncher.Extension.ConnectX.Utils;
using FluentLauncher.Extension.ConnectX.ViewModels;
using FluentLauncher.Extension.ConnectX.Views;
using FluentLauncher.Infra.ExtensionHost.Extensions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.UI.Xaml.Controls;
using Serilog;
using Serilog.Filters;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace FluentLauncher.Extension.ConnectX;

public class ConnectXExtension : IExtension, INavigationProviderExtension
{
    string IExtension.Name => "FluentLauncher.Extension.ConnectX";

    string IExtension.Description => "基于 ConnectX 开发的适用于 Fluent Launcher 的插件，提供用户友好的 UI 界面以便方便进行 Minecraft 远程联机";

    public static IServiceProvider? Services { get; private set; }

    public static string ExtensionFolder { get; private set; } = null!;

    public CancellationTokenSource CancellationTokenSource { get; private set; } = new();

    public Dictionary<string, (Type, Type)> RegisteredPages { get; } = new()
    {
        { "ConnectXPage", (typeof(ConnectXPage), typeof(ConnectXViewModel)) },
    };

    public Dictionary<string, (Type, Type)> RegisteredDialogs { get; } = new()
    {
        { "ConnectXCreateRoomDialog", (typeof(CreateRoomDialog), typeof(CreateRoomDialogViewModel)) },
        { "ConnectXJoinRoomDialog", (typeof(JoinRoomDialog), typeof(JoinRoomDialogViewModel)) },
    };

    void IExtension.ConfigureServices(IServiceCollection services)
    {
        services.AddSingleton<RoomService>();
        services.AddSingleton<ConnectService>();
        services.AddSingleton<AccountService>();
        services.AddSingleton<FakeMultiCasterService>();
        services.UseConnectX();

        services.AddSerilog(configure => 
        {
            configure.WriteTo.Logger(l =>
            {
                l.Filter.ByIncludingOnly(Matching.FromSource("ConnectX.Client"))
                    .WriteTo.File(Path.Combine(ExtensionFolder, "ConnectX.Client", "Logs", "Log-.log"),
                        rollingInterval: RollingInterval.Day,
                        rollOnFileSizeLimit: true,
                        fileSizeLimitBytes: 1000000,
                        outputTemplate: "[{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz}][{Level:u3}] <{SourceContext}>: {Message:lj}{NewLine}{Exception}");
            });
        });
    }

    void IExtension.SetServiceProvider(IServiceProvider serviceProvider)
    {
        Services = serviceProvider;

        List<IHostedService> backgroundServices =
        [
            serviceProvider.GetRequiredService<Router>(),
            serviceProvider.GetRequiredService<IZeroTierNodeLinkHolder>(),
            serviceProvider.GetRequiredService<IServerLinkHolder>(),
            serviceProvider.GetRequiredService<IRoomInfoManager>(),
            serviceProvider.GetRequiredService<PeerManager>(),
            serviceProvider.GetRequiredService<ProxyManager>(),
            serviceProvider.GetRequiredService<FakeServerMultiCaster>(),
        ];

        backgroundServices.ForEach(s => Task.Run(async () => await s.StartAsync(CancellationTokenSource.Token)));
    }

    void IExtension.SetExtensionFolder(string folder) => ExtensionFolder = folder;

    NavigationViewItem[] INavigationProviderExtension.ProvideNavigationItems() =>
    [
        new()
        {
            Icon = new SymbolIcon(Symbol.Link),
            Content = "多人游戏",
            Tag = "ConnectXPage"
        }
    ];
}

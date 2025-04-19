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
using System.Threading.Tasks;

namespace FluentLauncher.Extension.ConnectX;

public class ConnectXExtension : IExtension, INavigationProviderExtension
{
    string IExtension.Name => "FluentLauncher.Extension.ConnectX";

    string IExtension.Description => "[Preview] 适用于 Fluent Launcher 的 ConnectX 多人联机支持";

    public static IServiceProvider? Services { get; private set; }

    public static string ExtensionFolder { get; private set; } = null!;

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
            serviceProvider.GetRequiredService<IRoomInfoManager>(),
            serviceProvider.GetRequiredService<PeerManager>(),
            serviceProvider.GetRequiredService<ProxyManager>(),
            serviceProvider.GetRequiredService<FakeServerMultiCaster>(),
        ];

        backgroundServices.ForEach(s => Task.Run(async () => await s.StartAsync(default)));

        serviceProvider.GetService<ConnectService>();
        serviceProvider.GetService<AccountService>();
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

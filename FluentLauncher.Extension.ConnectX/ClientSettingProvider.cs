using ConnectX.Client.Interfaces;
using FluentLauncher.Infra.Settings;
using FluentLauncher.Infra.Settings.Converters;
using System;
using System.Linq;
using System.Net;
using System.Net.Sockets;

namespace FluentLauncher.Extension.ConnectX;

public partial class ClientSettingProvider(ISettingsStorage storage) 
    : SettingsContainer(storage), IClientSettingProvider
{
    [SettingItem(Default = "", Converter = typeof(JsonStringConverter<string>))]
    public partial string UserServerAddress { get; set; }

    [SettingItem(Default = 0, Converter = typeof(JsonStringConverter<int>))]
    public partial int ServerNodeSelection { get; set; }

    public IPAddress ServerAddress
    {
        get
        {
            return ServerNodeSelection switch
            {
                1 => IPAddress.Parse(MiaoVpsServerAddress),
                2 => IPAddress.Parse(UserServerAddress),
                _ => Dns.GetHostAddresses(BaseServerAddress).FirstOrDefault(ip => ip.AddressFamily == AddressFamily.InterNetwork)
                        ?? throw new ArgumentException("无法解析服务器地址"),
            };
        }
    }

    public ushort ServerPort => 3535;

    public bool JoinP2PNetwork => true;

    private const string BaseServerAddress = "";
    private const string MiaoVpsServerAddress = "";
}

using System.Net.Sockets;

namespace FluentLauncher.Extension.ConnectX.Model;

internal class MultiCasterServerInfo(AddressFamily addressFamily, string name, int port)
{
    public AddressFamily AddressFamily { get; init; } = addressFamily;

    public string Name { get; init; } = name;

    public int Port { get; init; } = port;
}

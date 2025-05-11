using FluentLauncher.Extension.ConnectX.Utils;
using System.Net;
using System.Text.Json.Serialization;

namespace FluentLauncher.Extension.ConnectX.Model;

public partial class InterconnectServer
{
    [JsonConverter(typeof(IPEndPointJsonConverter))]
    public required IPEndPoint ServerAddress { get; init; }

    public required string ServerName { get; init; }

    public required string ServerMotd { get; init; }
}
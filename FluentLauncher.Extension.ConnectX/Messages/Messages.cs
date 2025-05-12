using CommunityToolkit.Mvvm.Messaging.Messages;
using ConnectX.Shared.Messages.Server;
using FluentLauncher.Extension.ConnectX.Model;

namespace FluentLauncher.Extension.ConnectX.Messages;

internal class ServerConnectFailedMessage();

internal class ServerConnectStatusChangedMessage(ServerConnectStatus ConnectStatus) : ValueChangedMessage<ServerConnectStatus>(ConnectStatus);

internal class RoomOperatingMessage(bool IsOperating) : ValueChangedMessage<bool>(IsOperating);

internal class RoomStateChangedMessage(bool IsInRoom) : ValueChangedMessage<bool>(IsInRoom);

internal class RoomInfoUpdatedMessage();

internal class LanMultiCasterListenedMessage();

internal class InterconnectStatusChangedMessage(bool isInterconnected, InterconnectServerRegistration? interconnectServer) : ValueChangedMessage<bool>(isInterconnected)
{
    public InterconnectServerRegistration? InterconnectServer { get; set; } = interconnectServer;
};
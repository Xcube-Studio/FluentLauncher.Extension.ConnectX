using CommunityToolkit.Mvvm.Messaging.Messages;
using FluentLauncher.Extension.ConnectX.Model;

namespace FluentLauncher.Extension.ConnectX.Messages;

internal class ServerConnectFailedMessage();

internal class ServerStateChangedMessage(ServerConnectStatus ConnectStatus) : ValueChangedMessage<ServerConnectStatus>(ConnectStatus);

internal class RoomStateChangedMessage(bool IsInRoom) : ValueChangedMessage<bool>(IsInRoom);

internal class RoomInfoUpdatedMessage();



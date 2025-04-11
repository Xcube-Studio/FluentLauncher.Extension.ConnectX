using CommunityToolkit.Mvvm.Messaging;
using ConnectX.Client.Interfaces;
using ConnectX.Shared.Messages.Group;
using ConnectX.Shared.Models;
using FluentLauncher.Extension.ConnectX.Messages;
using System.Threading.Tasks;

using ConnectXClient = ConnectX.Client.Client;

namespace FluentLauncher.Extension.ConnectX.Services;

internal class RoomService
{
    private readonly ConnectXClient _client;
    private readonly IRoomInfoManager _roomInfoManager;
    private readonly IServerLinkHolder _serverLinkHolder;

    public RoomService(ConnectXClient client, IServerLinkHolder serverLinkHolder, IRoomInfoManager roomInfoManager)
    {
        _client = client;
        _serverLinkHolder = serverLinkHolder;
        _roomInfoManager = roomInfoManager;

        _serverLinkHolder.OnServerLinkDisconnected += OnServerLinkDisconnected;
        _client.OnGroupStateChanged += OnMemberBehaviorOccurred;
        _roomInfoManager.OnGroupInfoUpdated += OnGroupInfoUpdated;
    }

    public bool IsInRoom 
    { 
        get => field; 
        private set 
        { 
            field = value;
            WeakReferenceMessenger.Default.Send(new RoomStateChangedMessage(field));
        }
    }

    public GroupInfo? GroupInfo => _roomInfoManager.CurrentGroupInfo;

    public async Task<(GroupCreationStatus, string?)> CreateRoomAsync(CreateGroup createGroup)
    {
        var (_, status, error) = await _client.CreateGroupAsync(createGroup, default);

        if (status == GroupCreationStatus.Succeeded)
            IsInRoom = true;

        return (status, error);
    }

    public async Task<(GroupCreationStatus, string?)> JoinRoomAsync(JoinGroup joinGroup)
    {
        var (_, status, error) = await _client.JoinGroupAsync(joinGroup, default);

        if (status == GroupCreationStatus.Succeeded)
            IsInRoom = true;

        return (status, error);
    }

    public async Task<(bool, string?)> LeaveRoom()
    {
        LeaveGroup leaveGroup = new()
        {
            GroupId = GroupInfo!.RoomId,
            UserId = _serverLinkHolder.UserId
        };

        var (success, error) = await _client.LeaveGroupAsync(leaveGroup);

        if (success)
            IsInRoom = false;

        return (success, error);
    }

    private void OnServerLinkDisconnected() => IsInRoom = false;

    private void OnMemberBehaviorOccurred(GroupUserStates state, UserInfo? userInfo)
    {
        if (state == GroupUserStates.Dismissed)
        {
            IsInRoom = false;
            return;
        }
    }

    private void OnGroupInfoUpdated(GroupInfo obj) => WeakReferenceMessenger.Default.Send(new RoomInfoUpdatedMessage());
}

using CommunityToolkit.Mvvm.Messaging;
using ConnectX.Client.Interfaces;
using ConnectX.Shared.Helpers;
using ConnectX.Shared.Messages.Group;
using ConnectX.Shared.Models;
using FluentLauncher.Extension.ConnectX.Messages;
using System;
using System.Threading.Tasks;

using ConnectXClient = ConnectX.Client.Client;

namespace FluentLauncher.Extension.ConnectX.Services;

internal class RoomService
{
    private readonly ConnectXClient _client;
    private readonly AccountService _accountService;
    private readonly IRoomInfoManager _roomInfoManager;
    private readonly IServerLinkHolder _serverLinkHolder;

    public RoomService(ConnectXClient client, AccountService accountService, IServerLinkHolder serverLinkHolder, IRoomInfoManager roomInfoManager)
    {
        _client = client;
        _accountService = accountService;
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

    public bool IsOperatingRoom
    {
        get => field;
        private set
        {
            field = value;
            WeakReferenceMessenger.Default.Send(new RoomOperatingMessage(field));
        }
    }

    public GroupInfo? GroupInfo => _roomInfoManager.CurrentGroupInfo;

    public async Task<(GroupCreationStatus, string?)> CreateRoomAsync(CreateGroup createGroup)
    {
        IsOperatingRoom = true;

        try
        {
            var (_, status, error) = await _client.CreateGroupAsync(createGroup, default);

            if (status == GroupCreationStatus.Succeeded)
                IsInRoom = true;

            _client.UpdateDisplayNameAsync(_accountService.GetActiveAccountDisplayName(), default).Forget();

            return (status, error);
        }
        finally
        {
            IsOperatingRoom = false;
        }
    }

    public async Task<(GroupCreationStatus, string?)> JoinRoomAsync(JoinGroup joinGroup)
    {
        IsOperatingRoom = true;

        try
        {
            var (_, status, error) = await _client.JoinGroupAsync(joinGroup, default);

            if (status == GroupCreationStatus.Succeeded)
                IsInRoom = true;

            _client.UpdateDisplayNameAsync(_accountService.GetActiveAccountDisplayName(), default).Forget();

            return (status, error);
        }
        finally
        {
            IsOperatingRoom = false;
        }
    }

    public async Task<(bool, string?)> LeaveRoom()
    {
        IsOperatingRoom = true;

        try
        {
            var (success, error) = await _client.LeaveGroupAsync(new());

            if (success)
                IsInRoom = false;

            return (success, error);
        }
        finally
        {
            IsOperatingRoom = false;
        }
    }

    public async Task KickUserAsync(Guid guid)
    {
        KickUser kickUser = new()
        {
            UserToKick = guid
        };

        await _client.KickUserAsync(kickUser);
    }

    private void OnServerLinkDisconnected() => IsInRoom = false;

    private void OnMemberBehaviorOccurred(GroupUserStates state, UserInfo? userInfo)
    {
        WeakReferenceMessenger.Default.Send(new RoomInfoUpdatedMessage());

        if (state == GroupUserStates.Dismissed 
            || (state == GroupUserStates.Kicked && userInfo?.UserId == _serverLinkHolder.UserId))
        {
            IsInRoom = false;
            return;
        }
    }

    private void OnGroupInfoUpdated(GroupInfo obj) => WeakReferenceMessenger.Default.Send(new RoomInfoUpdatedMessage());
}

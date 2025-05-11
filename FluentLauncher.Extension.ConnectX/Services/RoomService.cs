using CommunityToolkit.Mvvm.Messaging;
using ConnectX.Client.Interfaces;
using ConnectX.Shared.Helpers;
using ConnectX.Shared.Messages.Group;
using ConnectX.Shared.Models;
using FluentLauncher.Extension.ConnectX.Messages;
using FluentLauncher.Extension.ConnectX.Model;
using FluentLauncher.Infra.UI.Notification;
using System;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;

using ConnectXClient = ConnectX.Client.Client;

namespace FluentLauncher.Extension.ConnectX.Services;

internal class RoomService
{
    private readonly ConnectXClient _client;
    private readonly AccountService _accountService;
    private readonly ConnectService _connectService;
    private readonly ClientSettingProvider _clientSettingProvider;

    private readonly IServerLinkHolder _serverLinkHolder;
    private readonly IRoomInfoManager _roomInfoManager;

    private readonly INotificationService _notificationService;

    public RoomService(
        ConnectXClient client,
        AccountService accountService, 
        ConnectService connectService, 
        ClientSettingProvider clientSettingProvider,
        IServerLinkHolder serverLinkHolder,
        IRoomInfoManager roomInfoManager,
        INotificationService notificationService)
    {
        _client = client;
        _accountService = accountService;
        _connectService = connectService;
        _clientSettingProvider = clientSettingProvider;

        _roomInfoManager = roomInfoManager;
        _serverLinkHolder = serverLinkHolder;

        _notificationService = notificationService;

        _client.OnGroupStateChanged += OnMemberBehaviorOccurred;
        _roomInfoManager.OnGroupInfoUpdated += OnGroupInfoUpdated;
        _serverLinkHolder.OnServerLinkDisconnected += OnServerLinkDisconnected;

        WeakReferenceMessenger.Default.Register<RoomStateChangedMessage>(this, async (r, m) =>
        {
            if (!m.Value && _clientSettingProvider.UseInterconnect)
                await connectService.RedirectToDefalutAsync();
        });
    }

    public bool IsInRoom 
    { 
        get => field; 
        private set 
        {
            if (field != value)
                WeakReferenceMessenger.Default.Send(new RoomStateChangedMessage(value));

            field = value;
        }
    }

    public bool IsOperatingRoom
    {
        get => field;
        private set
        {
            if (field != value)
                WeakReferenceMessenger.Default.Send(new RoomOperatingMessage(value));

            field = value;
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
            {
                IsInRoom = true;
                _client.UpdateDisplayNameAsync(_accountService.GetActiveAccountDisplayName(), default).Forget();

                return (status, error);
            }

            if (status == GroupCreationStatus.NeedRedirect)
            {
                _notificationService.JoinRoomNeedRedirect();
                return await RedirectAndJoinAsync(error!, joinGroup);
            }

            string reason = status switch
            {
                GroupCreationStatus.GroupNotExists => "不存在该房间，请检查你的邀请码",
                GroupCreationStatus.AlreadyInRoom => "已经在房间中了，请先退出该房间",
                GroupCreationStatus.GroupIsFull => "该房间已满",
                GroupCreationStatus.PasswordIncorrect => "房间密码不正确",
                _ => $"发生内部错误, {error}"
            };

            _notificationService.JoinRoomFailed(reason);
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

    private async Task<(GroupCreationStatus, string?)> RedirectAndJoinAsync(string server, JoinGroup joinGroup)
    {
        await _connectService.RedirectAsync(JsonSerializer.Deserialize<InterconnectServer>(server)
            ?? throw new InvalidDataException());

        return await JoinRoomAsync(joinGroup);
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

internal static class RoomServiceNotifications
{
    public static void JoinRoomFailed(this INotificationService notificationService, string reason)
    {
        notificationService.Show(new Notification
        {
            Title = "加入房间失败",
            Message = reason,
            Type = NotificationType.Error,
        });
    }

    public static void JoinRoomNeedRedirect(this INotificationService notificationService)
    {
        notificationService.Show(new Notification
        {
            Title = "加入房间中",
            Message = "已在其他服务节点上发现该房间，正在准备重定向连接",
            Type = NotificationType.Success,
        });
    }
}
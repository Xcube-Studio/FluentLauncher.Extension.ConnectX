using CommunityToolkit.Mvvm.Messaging;
using ConnectX.Client;
using ConnectX.Client.Interfaces;
using ConnectX.Shared.Messages.Group;
using ConnectX.Shared.Messages.Server;
using ConnectX.Shared.Models;
using FluentLauncher.Extension.ConnectX.Messages;
using FluentLauncher.Extension.ConnectX.Model;
using FluentLauncher.Extension.ConnectX.Views;
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
    private readonly ConnectService _connectService;
    private readonly ClientSettingProvider _clientSettingProvider;

    private readonly IServerLinkHolder _serverLinkHolder;
    private readonly IRoomInfoManager _roomInfoManager;

    private readonly INotificationService _notificationService;

    public RoomService(
        ConnectXClient client,
        ConnectService connectService, 
        ClientSettingProvider clientSettingProvider,
        IServerLinkHolder serverLinkHolder,
        IRoomInfoManager roomInfoManager,
        INotificationService notificationService)
    {
        _client = client;
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

    public async Task<OpResult> CreateRoomAsync(CreateGroup createGroup)
    {
        IsOperatingRoom = true;

        try
        {
            OpResult result = await _client.CreateGroupAsync(createGroup, default);

            if (result.Status == GroupCreationStatus.Succeeded)
                IsInRoom = true;

            return result;
        }
        finally
        {
            IsOperatingRoom = false;
        }
    }

    public async Task<OpResult> JoinRoomAsync(
        JoinGroup joinGroup, 
        Func<InterconnectServerRegistration, Task<bool>> requestRedirect)
    {
        IsOperatingRoom = true;

        try
        {
            var result = await _client.JoinGroupAsync(joinGroup, default);

            if (result.Status == GroupCreationStatus.Succeeded)
            {
                IsInRoom = true;

                return result;
            }

            if (result.Status == GroupCreationStatus.NeedRedirect)
            {
                _notificationService.JoinRoomNeedRedirect();
                return await RedirectAndJoinAsync(result, joinGroup, requestRedirect);
            }

            string reason = result.Status switch
            {
                GroupCreationStatus.GroupNotExists => "不存在该房间，请检查你的邀请码",
                GroupCreationStatus.AlreadyInRoom => "已经在房间中了，请先退出该房间",
                GroupCreationStatus.GroupIsFull => "该房间已满",
                GroupCreationStatus.PasswordIncorrect => "房间密码不正确",
                _ => $"发生内部错误, {result.Error}"
            };

            _notificationService.JoinRoomFailed(reason);
            return result;
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

    private async Task<OpResult> RedirectAndJoinAsync(
        OpResult result, 
        JoinGroup joinGroup, 
        Func<InterconnectServerRegistration, Task<bool>> requestRedirect)
    {
        if (!result.Metadata.TryGetValue(GroupOpResult.MetadataRedirectInfo, out string? server))
            throw new InvalidDataException();

        InterconnectServerRegistration interconnectServer = JsonSerializer.Deserialize<InterconnectServerRegistration>(server)
            ?? throw new InvalidDataException();

        if (!await requestRedirect(interconnectServer))
        {
            _notificationService.JoinRoomCancelRedirect();
            return result;
        }

        await _connectService.RedirectAsync(interconnectServer);
        return await JoinRoomAsync(joinGroup, _ => Task.FromResult(false)); // 不允许二次重定向
    }

    private void OnServerLinkDisconnected() => IsInRoom = false;

    private void OnMemberBehaviorOccurred(GroupUserStates state, UserInfo? userInfo)
    {
        WeakReferenceMessenger.Default.Send(new RoomInfoUpdatedMessage());

        if (state == GroupUserStates.Dismissed)
        {
            IsInRoom = false;
            _notificationService.RoomDismissed();
            return;
        }
        else if (state == GroupUserStates.Kicked && userInfo?.UserId == _serverLinkHolder.UserId)
        {
            IsInRoom = false;
            _notificationService.KickedFromRoom();
            return;
        }

        switch (state)
        {
            case GroupUserStates.Joined:
                _notificationService.MemberJoinRoom(userInfo!);
                break;
            case GroupUserStates.Left:
            case GroupUserStates.Kicked:
            case GroupUserStates.Disconnected:
                _notificationService.MemberLeaveRoom(userInfo!);
                break;
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

    public static void JoinRoomCancelRedirect(this INotificationService notificationService)
    {
        notificationService.Show(new Notification
        {
            Title = "已取消加入房间",
            Message = "已拒绝重定向到该服务节点",
        });
    }

    public static void MemberJoinRoom(this INotificationService notificationService, UserInfo userInfo)
        => notificationService.Show(new TeachingTipNotification { Title = $"成员 {ConnectXPage.GetNameFromDisplayName(userInfo.DisplayName)} 正在加入房间", Icon = "\ue946" });

    public static void MemberLeaveRoom(this INotificationService notificationService, UserInfo userInfo)
        => notificationService.Show(new TeachingTipNotification { Title = $"成员 {ConnectXPage.GetNameFromDisplayName(userInfo.DisplayName)} 离开房间", Icon = "\ue946" });

    public static void RoomDismissed(this INotificationService notificationService)
        => notificationService.Show(new TeachingTipNotification { Title = "房间已被解散", Icon = "\ue946" });

    public static void KickedFromRoom(this INotificationService notificationService) 
        => notificationService.Show(new TeachingTipNotification { Title = "你已被踢出房间", Icon = "\ue946" });
}
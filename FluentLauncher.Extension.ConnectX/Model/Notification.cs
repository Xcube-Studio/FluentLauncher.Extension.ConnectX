using FluentLauncher.Infra.UI.Notification;
using Microsoft.UI.Xaml.Controls;

namespace FluentLauncher.Extension.ConnectX.Model;

class Notification : INotification<InfoBar>
{
    public NotificationType Type { get; init; } = NotificationType.Info;

    public required string Title { get; init; }

    public string? Message { get; init; }

    public bool IsClosable { get; init; } = true;

    public double Delay { get; init; } = 5;

    InfoBar INotification<InfoBar>.ConstructUI()
    {
        InfoBar infoBar = new()
        {
            Title = Title,
            Message = Message,
            IsOpen = true,
            IsClosable = IsClosable,
            Translation = new System.Numerics.Vector3(0, 0, 16),
            Severity = Type switch
            {
                NotificationType.Info => InfoBarSeverity.Informational,
                NotificationType.Warning => InfoBarSeverity.Warning,
                NotificationType.Error => InfoBarSeverity.Error,
                NotificationType.Success => InfoBarSeverity.Success,
                _ => InfoBarSeverity.Informational
            }
        };

        infoBar.Closing += (_, _) => infoBar.IsOpen = true;

        return infoBar;
    }
}

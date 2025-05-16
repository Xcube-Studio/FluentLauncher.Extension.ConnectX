using FluentLauncher.Infra.UI.Notification;
using Microsoft.UI.Xaml;
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

class TeachingTipNotification : INotification<TeachingTip>
{
    bool INotification.IsClosable => true;

    NotificationType INotification.Type => NotificationType.Info;

    public string? Icon { get; init; }

    public required string Title { get; init; }

    public string? Message { get; init; }

    public double Delay { get; init; } = 7.5;

    public string? CloseButtonContent { get; init; }

    TeachingTip INotification<TeachingTip>.ConstructUI()
    {
        TeachingTip teachingTip = new()
        {
            Title = Title,
            Subtitle = Message,
            CloseButtonContent = CloseButtonContent,
            PreferredPlacement = TeachingTipPlacementMode.Auto,
            IsLightDismissEnabled = true,
            PlacementMargin = new Thickness(48),
        };

        if (!string.IsNullOrEmpty(Icon))
            teachingTip.IconSource = new FontIconSource() { Glyph = Icon };

        return teachingTip;
    }
}

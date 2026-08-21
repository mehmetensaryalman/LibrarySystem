namespace LibrarySystem.Application.DTOs.Notifications;

public class NotificationSummaryDto
{
    public int UnreadCount
    {
        get;
        set;
    }

    public IReadOnlyList<NotificationDto>
        Notifications
    {
        get;
        set;
    } = [];
}
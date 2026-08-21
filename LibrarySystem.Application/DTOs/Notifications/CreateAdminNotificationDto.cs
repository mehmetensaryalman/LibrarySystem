using LibrarySystem.Domain.Enums;

namespace LibrarySystem.Application.DTOs.Notifications;

public class CreateAdminNotificationDto
{
    public NotificationType Type
    {
        get;
        set;
    }

    public string Title
    {
        get;
        set;
    } = string.Empty;

    public string Message
    {
        get;
        set;
    } = string.Empty;

    public int? BorrowRecordId
    {
        get;
        set;
    }
}
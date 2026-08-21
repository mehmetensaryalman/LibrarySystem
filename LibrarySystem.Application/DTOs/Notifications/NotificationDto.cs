namespace LibrarySystem.Application.DTOs.Notifications;

public class NotificationDto
{
    public int Id { get; set; }

    public string Type { get; set; }
        = string.Empty;

    public string Title { get; set; }
        = string.Empty;

    public string Message { get; set; }
        = string.Empty;

    public int? BorrowRecordId
    {
        get;
        set;
    }

    public bool IsRead { get; set; }

    public DateTime CreatedAt
    {
        get;
        set;
    }

    public DateTime? ReadAt
    {
        get;
        set;
    }
}
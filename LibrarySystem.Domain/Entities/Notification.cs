using LibrarySystem.Domain.Enums;

namespace LibrarySystem.Domain.Entities;

public class Notification
{
    public int Id
    {
        get;
        set;
    }

    public string RecipientUserId
    {
        get;
        set;
    } = string.Empty;

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

    public bool IsRead
    {
        get;
        set;
    }

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

    public BorrowRecord? BorrowRecord
    {
        get;
        set;
    }
}
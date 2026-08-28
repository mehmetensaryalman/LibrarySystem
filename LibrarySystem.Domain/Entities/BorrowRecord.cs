namespace LibrarySystem.Domain.Entities;

public class BorrowRecord
{
    public int Id { get; set; }

    public string UserId { get; set; }
        = string.Empty;

    public int BookId { get; set; }

    public DateTime BorrowDate { get; set; }

    public DateTime DueDate { get; set; }

    public DateTime? ReturnRequestedAt
    {
        get;
        set;
    }

    public DateTime? ReturnDate
    {
        get;
        set;
    }

    public string? ReturnedToAdminUserId
    {
        get;
        set;
    }

    public bool IsReturned
    {
        get;
        set;
    }

    public DateTime? ThreeDaysReminderSentAt
    {
        get;
        set;
    }

    public DateTime? DueDateReminderSentAt
    {
        get;
        set;
    }

    public DateTime? OverdueReminderSentAt
    {
        get;
        set;
    }

    public Book Book
    {
        get;
        set;
    } = null!;

    public BorrowPenalty? Penalty
    {
        get;
        set;
    }
}

namespace LibrarySystem.Domain.Entities;

public class BorrowPenalty
{
    public int Id { get; set; }

    public string UserId { get; set; }
        = string.Empty;

    public int BorrowRecordId { get; set; }

    public int PenaltyDays { get; set; }

    public DateTime StartDate { get; set; }

    public DateTime EndDate { get; set; }

    public DateTime CreatedAt { get; set; }

    public BorrowRecord BorrowRecord
    {
        get;
        set;
    } = null!;
}
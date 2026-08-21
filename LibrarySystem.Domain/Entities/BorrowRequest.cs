using LibrarySystem.Domain.Enums;

namespace LibrarySystem.Domain.Entities;

public class BorrowRequest
{
    public int Id { get; set; }

    public string UserId
    {
        get;
        set;
    } = string.Empty;

    public int BookId
    {
        get;
        set;
    }

    public BorrowRequestStatus Status
    {
        get;
        set;
    }

    public DateTime RequestedAt
    {
        get;
        set;
    }

    public DateTime? ProcessedAt
    {
        get;
        set;
    }

    public string? ProcessedByAdminUserId
    {
        get;
        set;
    }

    public int? BorrowRecordId
    {
        get;
        set;
    }

    public string? RejectionReason
    {
        get;
        set;
    }

    public Book Book
    {
        get;
        set;
    } = null!;

    public BorrowRecord? BorrowRecord
    {
        get;
        set;
    }
}
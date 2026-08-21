namespace LibrarySystem.Application.Common.Models;

public enum ApproveBorrowRequestWriteStatus
{
    Success,
    PendingRequestNotFound,
    BookUnavailable,
    DuplicateActiveBorrow,
    OverdueActiveBorrow,
    ActivePenalty,
    ActiveBorrowLimitReached
}

public class ApproveBorrowRequestWriteResult
{
    public ApproveBorrowRequestWriteStatus Status
    {
        get;
        set;
    }

    public int? BorrowRecordId
    {
        get;
        set;
    }

    public string? UserId
    {
        get;
        set;
    }

    public int? BookId
    {
        get;
        set;
    }
}
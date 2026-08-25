namespace LibrarySystem.Application.Common.Models;

public enum BorrowRequestWriteStatus
{
    Success,
    DuplicatePendingRequest,
    BorrowLimitReached,
    CancellationCooldownActive
}
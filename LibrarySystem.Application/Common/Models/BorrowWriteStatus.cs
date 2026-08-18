namespace LibrarySystem.Application.Common.Models;

public enum BorrowWriteStatus
{
    Success,
    BookUnavailable,
    DuplicateActiveBorrow
}
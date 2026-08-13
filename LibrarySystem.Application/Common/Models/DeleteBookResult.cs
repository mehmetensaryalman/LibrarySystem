namespace LibrarySystem.Application.Common.Models;

public enum DeleteBookStatus
{
    Deleted,
    Archived,
    NotFound,
    ActiveBorrowExists
}

public class DeleteBookResult
{
    public DeleteBookStatus Status { get; set; }

    public string Message { get; set; } = string.Empty;
}
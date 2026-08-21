namespace LibrarySystem.Application.DTOs.Borrow;

public class AdminBorrowResponseDto
{
    public int BorrowRecordId { get; set; }

    public string UserId { get; set; } = string.Empty;

    public string UserEmail { get; set; } = string.Empty;

    public int BookId { get; set; }

    public string BookName { get; set; } = string.Empty;

    public string Author { get; set; } = string.Empty;

    public DateTime BorrowDate { get; set; }

    public DateTime DueDate { get; set; }

    public DateTime? ReturnRequestedAt { get; set; }

    public DateTime? ReturnDate { get; set; }

    public string? ReturnedToAdminUserId { get; set; }

    public bool IsReturned { get; set; }
}
namespace LibrarySystem.Application.DTOs.Borrow;

public class BorrowedBookResponseDto
{
    public int BorrowRecordId { get; set; }

    public int BookId { get; set; }

    public string BookName { get; set; } = string.Empty;

    public string Author { get; set; } = string.Empty;

    public DateTime BorrowDate { get; set; }

    public DateTime DueDate { get; set; }

    public DateTime? ReturnDate { get; set; }

    public bool IsReturned { get; set; }
}
namespace LibrarySystem.Application.DTOs.Borrow;

public class AdminBorrowNotificationDto
{
    public int BookId { get; set; }

    public string BookName { get; set; }
        = string.Empty;

    public string UserEmail { get; set; }
        = string.Empty;

    public DateTime BorrowDate { get; set; }
}
namespace LibrarySystem.Domain.Entities;

public class Book
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string Author { get; set; } = string.Empty;

    public int Stock { get; set; }

    public ICollection<BorrowRecord> BorrowRecords { get; set; } = new List<BorrowRecord>();
}
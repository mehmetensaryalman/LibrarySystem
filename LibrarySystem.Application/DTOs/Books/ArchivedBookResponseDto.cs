namespace LibrarySystem.Application.DTOs.Books;

public class ArchivedBookResponseDto
{
    public int Id { get; set; }

    public string Name { get; set; }
        = string.Empty;

    public string Author { get; set; }
        = string.Empty;

    public int Stock { get; set; }

    public bool IsArchived { get; set; }

    public DateTime? ArchivedAt { get; set; }
}
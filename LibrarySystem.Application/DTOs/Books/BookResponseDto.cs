namespace LibrarySystem.Application.DTOs.Books;

public class BookResponseDto
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string Author { get; set; } = string.Empty;

    public int Stock { get; set; }
}
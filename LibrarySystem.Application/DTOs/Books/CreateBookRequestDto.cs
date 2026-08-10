using System.ComponentModel.DataAnnotations;

namespace LibrarySystem.Application.DTOs.Books;

public class CreateBookRequestDto
{
    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    [Required]
    [MaxLength(150)]
    public string Author { get; set; } = string.Empty;

    [Range(0, int.MaxValue)]
    public int Stock { get; set; }
}
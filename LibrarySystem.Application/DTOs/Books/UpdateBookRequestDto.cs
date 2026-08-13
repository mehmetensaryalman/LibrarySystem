using System.ComponentModel.DataAnnotations;

namespace LibrarySystem.Application.DTOs.Books;

public class UpdateBookRequestDto
{
    [Required(
        ErrorMessage = "Kitap adı zorunludur."
    )]
    [MaxLength(
        200,
        ErrorMessage = "Kitap adı en fazla 200 karakter olabilir."
    )]
    public string Name { get; set; } = string.Empty;

    [Required(
        ErrorMessage = "Yazar adı zorunludur."
    )]
    [MaxLength(
        150,
        ErrorMessage = "Yazar adı en fazla 150 karakter olabilir."
    )]
    [RegularExpression(
        @"^(?=.*\p{L})[\p{L}\p{M}.'’\- ]+$",
        ErrorMessage =
            "Yazar adı sayı veya geçersiz karakter içeremez."
    )]
    public string Author { get; set; } = string.Empty;

    [Range(
        0,
        1000,
        ErrorMessage =
            "Stok 0 ile 1000 arasında olmalıdır."
    )]
    public int Stock { get; set; }
}
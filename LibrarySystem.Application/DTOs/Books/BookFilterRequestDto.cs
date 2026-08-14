using System.ComponentModel.DataAnnotations;

namespace LibrarySystem.Application.DTOs.Books;

public class BookFilterRequestDto
{
    [MaxLength(
        200,
        ErrorMessage =
            "Arama metni en fazla 200 karakter olabilir.")]
    public string? Search { get; set; }

    public bool? InStock { get; set; }

    [RegularExpression(
        "^(newest|nameAsc)$",
        ErrorMessage =
            "Geçersiz sıralama seçeneği.")]
    public string SortBy { get; set; } =
        "newest";

    [Range(
        1,
        1000000,
        ErrorMessage =
            "Sayfa numarası en az 1 olmalıdır.")]
    public int PageNumber { get; set; } = 1;

    [Range(
        1,
        100,
        ErrorMessage =
            "Sayfa boyutu 1 ile 100 arasında olmalıdır.")]
    public int PageSize { get; set; } = 10;
}
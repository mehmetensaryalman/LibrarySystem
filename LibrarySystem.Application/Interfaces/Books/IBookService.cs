using LibrarySystem.Application.DTOs.Books;
using LibrarySystem.Application.Common.Models;

namespace LibrarySystem.Application.Interfaces.Books;

public interface IBookService
{
    Task<List<BookResponseDto>> GetAllAsync();

    Task<List<ArchivedBookResponseDto>>
        GetArchivedAsync();

    Task<BookResponseDto> CreateAsync(
        CreateBookRequestDto request);

    Task<BookResponseDto?> UpdateAsync(
        int id,
        UpdateBookRequestDto request);

    Task<DeleteBookResult> DeleteAsync(int id);

    Task<ArchivedBookResponseDto?>
        RestoreAsync(int id);
}
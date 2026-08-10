using LibrarySystem.Application.DTOs.Books;

namespace LibrarySystem.Application.Interfaces.Books;

public interface IBookService
{
    Task<List<BookResponseDto>> GetAllAsync();

    Task<BookResponseDto> CreateAsync(CreateBookRequestDto request);

    Task<bool> DeleteAsync(int id);
}
using LibrarySystem.Application.DTOs.Books;
using LibrarySystem.Application.Interfaces.Books;
using LibrarySystem.Application.Interfaces.Repositories;
using LibrarySystem.Domain.Entities;

namespace LibrarySystem.Application.Services.Books;

public class BookService : IBookService
{
    private readonly IBookRepository _bookRepository;

    public BookService(IBookRepository bookRepository)
    {
        _bookRepository = bookRepository;
    }

    public async Task<List<BookResponseDto>> GetAllAsync()
    {
        var books = await _bookRepository.GetAllAsync();

        return books
            .Select(book => new BookResponseDto
            {
                Id = book.Id,
                Name = book.Name,
                Author = book.Author,
                Stock = book.Stock
            })
            .ToList();
    }

    public async Task<BookResponseDto> CreateAsync(
        CreateBookRequestDto request)
    {
        var book = new Book
        {
            Name = request.Name.Trim(),
            Author = request.Author.Trim(),
            Stock = request.Stock
        };

        await _bookRepository.AddAsync(book);

        return new BookResponseDto
        {
            Id = book.Id,
            Name = book.Name,
            Author = book.Author,
            Stock = book.Stock
        };
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var book = await _bookRepository.GetByIdAsync(id);

        if (book is null)
        {
            return false;
        }

        await _bookRepository.DeleteAsync(book);

        return true;
    }
}
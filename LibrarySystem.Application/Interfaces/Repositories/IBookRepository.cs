using LibrarySystem.Application.Common.Models;
using LibrarySystem.Domain.Entities;

namespace LibrarySystem.Application.Interfaces.Repositories;

public interface IBookRepository
{
    Task<List<Book>> GetAllAsync();

    Task<PagedResult<Book>> GetPagedAsync(
        string? search,
        bool? inStock,
        string sortBy,
        int pageNumber,
        int pageSize);

    Task<List<Book>> GetArchivedAsync();

    Task<Book?> GetByIdAsync(int id);

    Task<Book?> GetArchivedByIdAsync(int id);

    Task<bool> ExistsByNameAndAuthorAsync(
        string name,
        string author,
        int? excludedBookId = null);

    Task<Book> AddAsync(Book book);

    Task<Book> UpdateAsync(Book book);

    Task DeleteAsync(Book book);

    Task<bool> HasActiveBorrowAsync(
        int bookId);

    Task<bool> HasBorrowHistoryAsync(
        int bookId);

    Task ArchiveAsync(Book book);

    Task RestoreAsync(Book book);
}
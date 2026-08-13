using LibrarySystem.Application.Interfaces.Repositories;
using LibrarySystem.Domain.Entities;
using LibrarySystem.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace LibrarySystem.Infrastructure.Repositories;

public class BookRepository : IBookRepository
{
    private readonly LibraryDbContext _dbContext;

    public BookRepository(
        LibraryDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<List<Book>> GetAllAsync()
    {
        return await _dbContext.Books
            .AsNoTracking()
            .Where(book => !book.IsArchived)
            .OrderBy(book => book.Name)
            .ToListAsync();
    }

    public async Task<List<Book>> GetArchivedAsync()
    {
        return await _dbContext.Books
            .AsNoTracking()
            .Where(book => book.IsArchived)
            .OrderBy(book => book.Name)
            .ToListAsync();
    }

    public async Task<Book?> GetByIdAsync(
        int id)
    {
        return await _dbContext.Books
            .FirstOrDefaultAsync(book =>
                book.Id == id &&
                !book.IsArchived);
    }

    public async Task<Book?> GetArchivedByIdAsync(
        int id)
    {
        return await _dbContext.Books
            .FirstOrDefaultAsync(book =>
                book.Id == id &&
                book.IsArchived);
    }

    public async Task<Book> AddAsync(
        Book book)
    {
        await _dbContext.Books
            .AddAsync(book);

        await _dbContext.SaveChangesAsync();

        return book;
    }

    public async Task<Book> UpdateAsync(
        Book book)
    {
        await _dbContext.SaveChangesAsync();

        return book;
    }

    public async Task DeleteAsync(
        Book book)
    {
        _dbContext.Books.Remove(book);

        await _dbContext.SaveChangesAsync();
    }

    public async Task<bool> HasActiveBorrowAsync(
        int bookId)
    {
        return await _dbContext.BorrowRecords
            .AnyAsync(record =>
                record.BookId == bookId &&
                !record.IsReturned);
    }

    public async Task<bool> HasBorrowHistoryAsync(
        int bookId)
    {
        return await _dbContext.BorrowRecords
            .AnyAsync(record =>
                record.BookId == bookId);
    }

    public async Task ArchiveAsync(
        Book book)
    {
        book.IsArchived = true;

        await _dbContext.SaveChangesAsync();
    }

    public async Task RestoreAsync(
        Book book)
    {
        book.IsArchived = false;

        await _dbContext.SaveChangesAsync();
    }
}
using LibrarySystem.Application.Interfaces.Repositories;
using LibrarySystem.Domain.Entities;
using LibrarySystem.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace LibrarySystem.Infrastructure.Repositories;

public class BookRepository : IBookRepository
{
    private readonly LibraryDbContext _dbContext;

    public BookRepository(LibraryDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<List<Book>> GetAllAsync()
    {
        return await _dbContext.Books
            .AsNoTracking()
            .OrderBy(book => book.Name)
            .ToListAsync();
    }

    public async Task<Book?> GetByIdAsync(int id)
    {
        return await _dbContext.Books
            .FirstOrDefaultAsync(book => book.Id == id);
    }

    public async Task AddAsync(Book book)
    {
        await _dbContext.Books.AddAsync(book);
        await _dbContext.SaveChangesAsync();
    }

    public async Task DeleteAsync(Book book)
    {
        _dbContext.Books.Remove(book);
        await _dbContext.SaveChangesAsync();
    }
}
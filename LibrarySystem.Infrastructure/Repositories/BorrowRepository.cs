using LibrarySystem.Application.Interfaces.Repositories;
using LibrarySystem.Domain.Entities;
using LibrarySystem.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace LibrarySystem.Infrastructure.Repositories;

public class BorrowRepository : IBorrowRepository
{
    private readonly LibraryDbContext _dbContext;

    public BorrowRepository(LibraryDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Book?> GetBookByIdAsync(int bookId)
    {
        return await _dbContext.Books
            .FirstOrDefaultAsync(book => book.Id == bookId);
    }

    public async Task<BorrowRecord?> GetActiveBorrowAsync(
        string userId,
        int bookId)
    {
        return await _dbContext.BorrowRecords
            .FirstOrDefaultAsync(record =>
                record.UserId == userId &&
                record.BookId == bookId &&
                !record.IsReturned);
    }

    public async Task<List<BorrowRecord>> GetUserBorrowsAsync(
        string userId)
    {
        return await _dbContext.BorrowRecords
            .AsNoTracking()
            .Include(record => record.Book)
            .Where(record => record.UserId == userId)
            .OrderByDescending(record => record.BorrowDate)
            .ToListAsync();
    }

    public async Task AddBorrowAsync(
        BorrowRecord borrowRecord)
    {
        await _dbContext.BorrowRecords.AddAsync(borrowRecord);
    }

    public async Task SaveChangesAsync()
    {
        await _dbContext.SaveChangesAsync();
    }
}
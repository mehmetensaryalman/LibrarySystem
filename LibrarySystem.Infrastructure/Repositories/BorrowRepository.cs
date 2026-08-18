using LibrarySystem.Application.Common.Models;
using LibrarySystem.Application.Interfaces.Repositories;
using LibrarySystem.Domain.Entities;
using LibrarySystem.Infrastructure.Persistence;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace LibrarySystem.Infrastructure.Repositories;

public class BorrowRepository :
    IBorrowRepository
{
    private readonly
        LibraryDbContext _dbContext;

    public BorrowRepository(
        LibraryDbContext dbContext)
    {
        _dbContext =
            dbContext;
    }

    public async Task<Book?>
        GetBookByIdAsync(
            int bookId)
    {
        return await _dbContext.Books
            .AsNoTracking()
            .FirstOrDefaultAsync(book =>
                book.Id == bookId &&
                !book.IsArchived);
    }

    public async Task<BorrowRecord?>
        GetActiveBorrowAsync(
            string userId,
            int bookId)
    {
        return await _dbContext
            .BorrowRecords
            .AsNoTracking()
            .FirstOrDefaultAsync(record =>
                record.UserId ==
                    userId &&
                record.BookId ==
                    bookId &&
                !record.IsReturned);
    }

    public async Task<List<BorrowRecord>>
        GetUserBorrowsAsync(
            string userId)
    {
        return await _dbContext
            .BorrowRecords
            .AsNoTracking()
            .Include(record =>
                record.Book)
            .Where(record =>
                record.UserId ==
                    userId)
            .OrderByDescending(record =>
                record.BorrowDate)
            .ToListAsync();
    }

    public async Task<List<BorrowRecord>>
        GetAllBorrowsAsync()
    {
        return await _dbContext
            .BorrowRecords
            .AsNoTracking()
            .Include(record =>
                record.Book)
            .OrderByDescending(record =>
                record.BorrowDate)
            .ToListAsync();
    }

    public async Task<
        Dictionary<string, string>>
        GetUserEmailsAsync(
            IEnumerable<string> userIds)
    {
        var ids =
            userIds
                .Distinct()
                .ToList();

        if (ids.Count == 0)
        {
            return new Dictionary<
                string,
                string>();
        }

        return await _dbContext.Users
            .AsNoTracking()
            .Where(user =>
                ids.Contains(
                    user.Id))
            .ToDictionaryAsync(
                user =>
                    user.Id,

                user =>
                    user.Email ??
                    user.UserName ??
                    "Bilinmiyor");
    }

    public async Task<BorrowWriteStatus>
        BorrowBookAsync(
            BorrowRecord borrowRecord)
    {
        await using var transaction =
            await _dbContext.Database
                .BeginTransactionAsync();

        try
        {
            var affectedBookRows =
                await _dbContext.Books
                    .Where(book =>
                        book.Id ==
                            borrowRecord.BookId &&
                        !book.IsArchived &&
                        book.Stock > 0)
                    .ExecuteUpdateAsync(
                        setters =>
                            setters.SetProperty(
                                book =>
                                    book.Stock,

                                book =>
                                    book.Stock - 1));

            if (affectedBookRows == 0)
            {
                await transaction
                    .RollbackAsync();

                return
                    BorrowWriteStatus
                        .BookUnavailable;
            }

            await _dbContext
                .BorrowRecords
                .AddAsync(
                    borrowRecord);

            try
            {
                await _dbContext
                    .SaveChangesAsync();
            }
            catch (
                DbUpdateException exception)
                when (
                    IsDuplicateActiveBorrowViolation(
                        exception))
            {
                await transaction
                    .RollbackAsync();

                return
                    BorrowWriteStatus
                        .DuplicateActiveBorrow;
            }

            await transaction
                .CommitAsync();

            return
                BorrowWriteStatus
                    .Success;
        }
        catch
        {
            await transaction
                .RollbackAsync();

            throw;
        }
    }

    public async Task<ReturnWriteStatus>
        ReturnBookAsync(
            string userId,
            int bookId,
            DateTime returnDate)
    {
        await using var transaction =
            await _dbContext.Database
                .BeginTransactionAsync();

        try
        {
            var affectedBorrowRows =
                await _dbContext
                    .BorrowRecords
                    .Where(record =>
                        record.UserId ==
                            userId &&
                        record.BookId ==
                            bookId &&
                        !record.IsReturned)
                    .ExecuteUpdateAsync(
                        setters =>
                            setters
                                .SetProperty(
                                    record =>
                                        record.IsReturned,
                                    true)
                                .SetProperty(
                                    record =>
                                        record.ReturnDate,
                                    returnDate));

            if (affectedBorrowRows == 0)
            {
                await transaction
                    .RollbackAsync();

                return
                    ReturnWriteStatus
                        .ActiveBorrowNotFound;
            }

            var affectedBookRows =
                await _dbContext.Books
                    .Where(book =>
                        book.Id ==
                            bookId)
                    .ExecuteUpdateAsync(
                        setters =>
                            setters.SetProperty(
                                book =>
                                    book.Stock,

                                book =>
                                    book.Stock + 1));

            if (affectedBookRows == 0)
            {
                await transaction
                    .RollbackAsync();

                return
                    ReturnWriteStatus
                        .BookNotFound;
            }

            await transaction
                .CommitAsync();

            return
                ReturnWriteStatus
                    .Success;
        }
        catch
        {
            await transaction
                .RollbackAsync();

            throw;
        }
    }

    private static bool
        IsDuplicateActiveBorrowViolation(
            DbUpdateException exception)
    {
        if (
            exception.InnerException
            is not SqlException sqlException)
        {
            return false;
        }

        foreach (
            SqlError error
            in sqlException.Errors)
        {
            var isDuplicateError =
                error.Number == 2601 ||
                error.Number == 2627;

            var isActiveBorrowIndex =
                error.Message.Contains(
                    "UX_BorrowRecords_UserId_BookId_Active",
                    StringComparison
                        .OrdinalIgnoreCase);

            if (
                isDuplicateError &&
                isActiveBorrowIndex)
            {
                return true;
            }
        }

        return false;
    }
}
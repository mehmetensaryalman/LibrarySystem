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

    public async Task<BorrowRecord?>
        GetActiveBorrowByIdAsync(
            int borrowRecordId)
    {
        return await _dbContext
            .BorrowRecords
            .AsNoTracking()
            .FirstOrDefaultAsync(record =>
                record.Id ==
                    borrowRecordId &&
                !record.IsReturned);
    }

    public async Task<bool>
        HasOverdueActiveBorrowAsync(
            string userId,
            DateTime currentDate)
    {
        return await _dbContext
            .BorrowRecords
            .AsNoTracking()
            .AnyAsync(record =>
                record.UserId ==
                    userId &&
                !record.IsReturned &&
                record.DueDate <
                    currentDate);
    }

    public async Task<DateTime?>
        GetActivePenaltyEndDateAsync(
            string userId,
            DateTime currentDate)
    {
        return await _dbContext
            .BorrowPenalties
            .AsNoTracking()
            .Where(penalty =>
                penalty.UserId ==
                    userId &&
                penalty.EndDate >
                    currentDate)
            .MaxAsync(penalty =>
                (DateTime?)
                    penalty.EndDate);
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
            await LockUserAsync(
                borrowRecord.UserId);

            var hasOverdueBorrow =
                await _dbContext
                    .BorrowRecords
                    .AsNoTracking()
                    .AnyAsync(record =>
                        record.UserId ==
                            borrowRecord.UserId &&
                        !record.IsReturned &&
                        record.DueDate <
                            borrowRecord.BorrowDate);

            if (hasOverdueBorrow)
            {
                await transaction
                    .RollbackAsync();

                return
                    BorrowWriteStatus
                        .OverdueActiveBorrow;
            }

            var hasActivePenalty =
                await _dbContext
                    .BorrowPenalties
                    .AsNoTracking()
                    .AnyAsync(penalty =>
                        penalty.UserId ==
                            borrowRecord.UserId &&
                        penalty.EndDate >
                            borrowRecord.BorrowDate);

            if (hasActivePenalty)
            {
                await transaction
                    .RollbackAsync();

                return
                    BorrowWriteStatus
                        .ActivePenalty;
            }

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

    public async Task<ReturnBookWriteResult>
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
            /*
             * Aynı kullanıcı için borrow/return/penalty
             * işlemlerini kısa süreli sıraya sokar.
             *
             * Böylece aynı kullanıcının iki kitabı
             * eşzamanlı geç iade edilirse cezalar
             * birbirinin üzerine yazılmaz.
             */
            await LockUserAsync(
                userId);

            var activeBorrow =
                await _dbContext
                    .BorrowRecords
                    .AsNoTracking()
                    .FirstOrDefaultAsync(record =>
                        record.UserId ==
                            userId &&
                        record.BookId ==
                            bookId &&
                        !record.IsReturned);

            if (activeBorrow is null)
            {
                await transaction
                    .RollbackAsync();

                return new ReturnBookWriteResult
                {
                    Status =
                        ReturnWriteStatus
                            .ActiveBorrowNotFound
                };
            }

            var affectedBorrowRows =
                await _dbContext
                    .BorrowRecords
                    .Where(record =>
                        record.Id ==
                            activeBorrow.Id &&
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

                return new ReturnBookWriteResult
                {
                    Status =
                        ReturnWriteStatus
                            .ActiveBorrowNotFound
                };
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

                return new ReturnBookWriteResult
                {
                    Status =
                        ReturnWriteStatus
                            .BookNotFound
                };
            }

            var penaltyDays = 0;

            DateTime? penaltyStartDate =
                null;

            DateTime? penaltyEndDate =
                null;

            if (
                returnDate >
                activeBorrow.DueDate)
            {
                var overdueDuration =
                    returnDate -
                    activeBorrow.DueDate;

                penaltyDays =
                    Math.Max(
                        1,
                        (int)Math.Ceiling(
                            overdueDuration
                                .TotalDays));

                var latestActivePenaltyEndDate =
                    await _dbContext
                        .BorrowPenalties
                        .AsNoTracking()
                        .Where(penalty =>
                            penalty.UserId ==
                                userId &&
                            penalty.EndDate >
                                returnDate)
                        .MaxAsync(penalty =>
                            (DateTime?)
                                penalty.EndDate);

                penaltyStartDate =
                    latestActivePenaltyEndDate ??
                    returnDate;

                penaltyEndDate =
                    penaltyStartDate.Value
                        .AddDays(
                            penaltyDays);

                var penalty =
                    new BorrowPenalty
                    {
                        UserId =
                            userId,

                        BorrowRecordId =
                            activeBorrow.Id,

                        PenaltyDays =
                            penaltyDays,

                        StartDate =
                            penaltyStartDate.Value,

                        EndDate =
                            penaltyEndDate.Value,

                        CreatedAt =
                            returnDate
                    };

                await _dbContext
                    .BorrowPenalties
                    .AddAsync(
                        penalty);

                await _dbContext
                    .SaveChangesAsync();
            }

            await transaction
                .CommitAsync();

            return new ReturnBookWriteResult
            {
                Status =
                    ReturnWriteStatus
                        .Success,

                PenaltyDays =
                    penaltyDays,

                PenaltyStartDate =
                    penaltyStartDate,

                PenaltyEndDate =
                    penaltyEndDate
            };
        }
        catch
        {
            await transaction
                .RollbackAsync();

            throw;
        }
    }

    private async Task
        LockUserAsync(
            string userId)
    {
        var user =
            await _dbContext.Users
                .FromSqlInterpolated(
                    $"""
                    SELECT *
                    FROM [AspNetUsers]
                    WITH (UPDLOCK, HOLDLOCK)
                    WHERE [Id] = {userId}
                    """)
                .AsNoTracking()
                .FirstOrDefaultAsync();

        if (user is null)
        {
            throw new InvalidOperationException(
                "Kullanıcı bulunamadı.");
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
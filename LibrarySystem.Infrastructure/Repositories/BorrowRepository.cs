using LibrarySystem.Application.Common.Constants;
using LibrarySystem.Application.Common.Models;
using LibrarySystem.Application.Interfaces.Repositories;
using LibrarySystem.Domain.Entities;
using LibrarySystem.Domain.Enums;
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

    public async Task<int>
        GetActiveBorrowCountAsync(
            string userId)
    {
        return await _dbContext
            .BorrowRecords
            .AsNoTracking()
            .CountAsync(record =>
                record.UserId ==
                    userId &&
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

    public async Task<BorrowRequest?>
        GetPendingBorrowRequestAsync(
            string userId,
            int bookId)
    {
        return await _dbContext
            .BorrowRequests
            .AsNoTracking()
            .Include(request =>
                request.Book)
            .FirstOrDefaultAsync(request =>
                request.UserId ==
                    userId &&
                request.BookId ==
                    bookId &&
                request.Status ==
                    BorrowRequestStatus
                        .Pending);
    }

    public async Task<BorrowRequest?>
        GetPendingBorrowRequestByIdAsync(
            int borrowRequestId)
    {
        return await _dbContext
            .BorrowRequests
            .AsNoTracking()
            .Include(request =>
                request.Book)
            .FirstOrDefaultAsync(request =>
                request.Id ==
                    borrowRequestId &&
                request.Status ==
                    BorrowRequestStatus
                        .Pending);
    }

    public async Task<List<BorrowRequest>>
        GetPendingBorrowRequestsAsync()
    {
        return await _dbContext
            .BorrowRequests
            .AsNoTracking()
            .Include(request =>
                request.Book)
            .Where(request =>
                request.Status ==
                    BorrowRequestStatus
                        .Pending)
            .OrderBy(request =>
                request.RequestedAt)
            .ThenBy(request =>
                request.Id)
            .ToListAsync();
    }

    public async Task<List<BorrowRequest>>
        GetPendingBorrowRequestsByUserAsync(
            string userId)
    {
        return await _dbContext
            .BorrowRequests
            .AsNoTracking()
            .Include(request =>
                request.Book)
            .Where(request =>
                request.UserId ==
                    userId &&
                request.Status ==
                    BorrowRequestStatus
                        .Pending)
            .OrderByDescending(request =>
                request.RequestedAt)
            .ThenByDescending(request =>
                request.Id)
            .ToListAsync();
    }

    public async Task<BorrowRequestWriteStatus>
        CreateBorrowRequestAsync(
            BorrowRequest borrowRequest)
    {
        await _dbContext
            .BorrowRequests
            .AddAsync(
                borrowRequest);

        try
        {
            await _dbContext
                .SaveChangesAsync();

            return
                BorrowRequestWriteStatus
                    .Success;
        }
        catch (
            DbUpdateException exception)
            when (
                IsDuplicatePendingBorrowRequestViolation(
                    exception))
        {
            _dbContext
                .Entry(
                    borrowRequest)
                .State =
                    EntityState.Detached;

            return
                BorrowRequestWriteStatus
                    .DuplicatePendingRequest;
        }
    }

    public async Task<ApproveBorrowRequestWriteResult>
        ApproveBorrowRequestAsync(
            int borrowRequestId,
            string adminUserId,
            DateTime approvalDate)
    {
        await using var transaction =
            await _dbContext.Database
                .BeginTransactionAsync();

        try
        {
            var borrowRequest =
                await LockBorrowRequestAsync(
                    borrowRequestId);

            if (
                borrowRequest is null ||
                borrowRequest.Status !=
                    BorrowRequestStatus.Pending)
            {
                await transaction
                    .RollbackAsync();

                return new ApproveBorrowRequestWriteResult
                {
                    Status =
                        ApproveBorrowRequestWriteStatus
                            .PendingRequestNotFound
                };
            }

            await LockUserAsync(
                borrowRequest.UserId);

            var hasOverdueBorrow =
                await _dbContext
                    .BorrowRecords
                    .AsNoTracking()
                    .AnyAsync(record =>
                        record.UserId ==
                            borrowRequest.UserId &&
                        !record.IsReturned &&
                        record.DueDate <
                            approvalDate);

            if (hasOverdueBorrow)
            {
                await transaction
                    .RollbackAsync();

                return new ApproveBorrowRequestWriteResult
                {
                    Status =
                        ApproveBorrowRequestWriteStatus
                            .OverdueActiveBorrow,

                    UserId =
                        borrowRequest.UserId,

                    BookId =
                        borrowRequest.BookId
                };
            }

            var hasActivePenalty =
                await _dbContext
                    .BorrowPenalties
                    .AsNoTracking()
                    .AnyAsync(penalty =>
                        penalty.UserId ==
                            borrowRequest.UserId &&
                        penalty.EndDate >
                            approvalDate);

            if (hasActivePenalty)
            {
                await transaction
                    .RollbackAsync();

                return new ApproveBorrowRequestWriteResult
                {
                    Status =
                        ApproveBorrowRequestWriteStatus
                            .ActivePenalty,

                    UserId =
                        borrowRequest.UserId,

                    BookId =
                        borrowRequest.BookId
                };
            }

            var hasDuplicateActiveBorrow =
                await _dbContext
                    .BorrowRecords
                    .AsNoTracking()
                    .AnyAsync(record =>
                        record.UserId ==
                            borrowRequest.UserId &&
                        record.BookId ==
                            borrowRequest.BookId &&
                        !record.IsReturned);

            if (hasDuplicateActiveBorrow)
            {
                await transaction
                    .RollbackAsync();

                return new ApproveBorrowRequestWriteResult
                {
                    Status =
                        ApproveBorrowRequestWriteStatus
                            .DuplicateActiveBorrow,

                    UserId =
                        borrowRequest.UserId,

                    BookId =
                        borrowRequest.BookId
                };
            }

            var activeBorrowCount =
                await _dbContext
                    .BorrowRecords
                    .AsNoTracking()
                    .CountAsync(record =>
                        record.UserId ==
                            borrowRequest.UserId &&
                        !record.IsReturned);

            if (
                activeBorrowCount >=
                BorrowRules
                    .MaxActiveBorrowCount)
            {
                await transaction
                    .RollbackAsync();

                return new ApproveBorrowRequestWriteResult
                {
                    Status =
                        ApproveBorrowRequestWriteStatus
                            .ActiveBorrowLimitReached,

                    UserId =
                        borrowRequest.UserId,

                    BookId =
                        borrowRequest.BookId
                };
            }

            var affectedBookRows =
                await _dbContext
                    .Books
                    .Where(book =>
                        book.Id ==
                            borrowRequest.BookId &&
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

                return new ApproveBorrowRequestWriteResult
                {
                    Status =
                        ApproveBorrowRequestWriteStatus
                            .BookUnavailable,

                    UserId =
                        borrowRequest.UserId,

                    BookId =
                        borrowRequest.BookId
                };
            }

            var borrowRecord =
                new BorrowRecord
                {
                    UserId =
                        borrowRequest.UserId,

                    BookId =
                        borrowRequest.BookId,

                    BorrowDate =
                        approvalDate,

                    DueDate =
                        approvalDate.AddDays(7),

                    ReturnRequestedAt =
                        null,

                    ReturnDate =
                        null,

                    ReturnedToAdminUserId =
                        null,

                    IsReturned =
                        false
                };

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

                return new ApproveBorrowRequestWriteResult
                {
                    Status =
                        ApproveBorrowRequestWriteStatus
                            .DuplicateActiveBorrow,

                    UserId =
                        borrowRequest.UserId,

                    BookId =
                        borrowRequest.BookId
                };
            }

            borrowRequest.Status =
                BorrowRequestStatus.Approved;

            borrowRequest.ProcessedAt =
                approvalDate;

            borrowRequest.ProcessedByAdminUserId =
                adminUserId;

            borrowRequest.BorrowRecordId =
                borrowRecord.Id;

            borrowRequest.RejectionReason =
                null;

            await _dbContext
                .SaveChangesAsync();

            await transaction
                .CommitAsync();

            return new ApproveBorrowRequestWriteResult
            {
                Status =
                    ApproveBorrowRequestWriteStatus
                        .Success,

                BorrowRecordId =
                    borrowRecord.Id,

                UserId =
                    borrowRequest.UserId,

                BookId =
                    borrowRequest.BookId
            };
        }
        catch
        {
            await transaction
                .RollbackAsync();

            throw;
        }
    }

    public async Task<RejectBorrowRequestWriteStatus>
        RejectBorrowRequestAsync(
            int borrowRequestId,
            string adminUserId,
            DateTime processedAt,
            string? rejectionReason)
    {
        await using var transaction =
            await _dbContext.Database
                .BeginTransactionAsync();

        try
        {
            var borrowRequest =
                await LockBorrowRequestAsync(
                    borrowRequestId);

            if (
                borrowRequest is null ||
                borrowRequest.Status !=
                    BorrowRequestStatus.Pending)
            {
                await transaction
                    .RollbackAsync();

                return
                    RejectBorrowRequestWriteStatus
                        .PendingRequestNotFound;
            }

            borrowRequest.Status =
                BorrowRequestStatus.Rejected;

            borrowRequest.ProcessedAt =
                processedAt;

            borrowRequest.ProcessedByAdminUserId =
                adminUserId;

            borrowRequest.BorrowRecordId =
                null;

            borrowRequest.RejectionReason =
                string.IsNullOrWhiteSpace(
                    rejectionReason)
                    ? null
                    : rejectionReason.Trim();

            await _dbContext
                .SaveChangesAsync();

            await transaction
                .CommitAsync();

            return
                RejectBorrowRequestWriteStatus
                    .Success;
        }
        catch
        {
            await transaction
                .RollbackAsync();

            throw;
        }
    }

    public async Task<ReturnRequestWriteStatus>
        RequestReturnAsync(
            string userId,
            int bookId,
            DateTime requestDate)
    {
        await using var transaction =
            await _dbContext.Database
                .BeginTransactionAsync();

        try
        {
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

                return
                    ReturnRequestWriteStatus
                        .ActiveBorrowNotFound;
            }

            if (
                activeBorrow
                    .ReturnRequestedAt
                    .HasValue)
            {
                await transaction
                    .RollbackAsync();

                return
                    ReturnRequestWriteStatus
                        .AlreadyRequested;
            }

            var affectedRows =
                await _dbContext
                    .BorrowRecords
                    .Where(record =>
                        record.Id ==
                            activeBorrow.Id &&
                        !record.IsReturned &&
                        record.ReturnRequestedAt ==
                            null)
                    .ExecuteUpdateAsync(
                        setters =>
                            setters.SetProperty(
                                record =>
                                    record.ReturnRequestedAt,
                                requestDate));

            if (affectedRows == 0)
            {
                var currentBorrow =
                    await _dbContext
                        .BorrowRecords
                        .AsNoTracking()
                        .FirstOrDefaultAsync(record =>
                            record.Id ==
                                activeBorrow.Id &&
                            !record.IsReturned);

                await transaction
                    .RollbackAsync();

                if (
                    currentBorrow?
                        .ReturnRequestedAt
                        .HasValue ==
                    true)
                {
                    return
                        ReturnRequestWriteStatus
                            .AlreadyRequested;
                }

                return
                    ReturnRequestWriteStatus
                        .ActiveBorrowNotFound;
            }

            await transaction
                .CommitAsync();

            return
                ReturnRequestWriteStatus
                    .Success;
        }
        catch
        {
            await transaction
                .RollbackAsync();

            throw;
        }
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

            var activeBorrowCount =
                await _dbContext
                    .BorrowRecords
                    .AsNoTracking()
                    .CountAsync(record =>
                        record.UserId ==
                            borrowRecord.UserId &&
                        !record.IsReturned);

            if (
                activeBorrowCount >=
                BorrowRules
                    .MaxActiveBorrowCount)
            {
                await transaction
                    .RollbackAsync();

                return
                    BorrowWriteStatus
                        .ActiveBorrowLimitReached;
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
            string adminUserId,
            DateTime returnDate)
    {
        await using var transaction =
            await _dbContext.Database
                .BeginTransactionAsync();

        try
        {
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
                                    returnDate)
                                .SetProperty(
                                    record =>
                                        record.ReturnedToAdminUserId,
                                    adminUserId));

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

    private async Task<BorrowRequest?>
        LockBorrowRequestAsync(
            int borrowRequestId)
    {
        return await _dbContext
            .BorrowRequests
            .FromSqlInterpolated(
                $"""
                    SELECT *
                    FROM [BorrowRequests]
                    WITH (UPDLOCK, HOLDLOCK)
                    WHERE [Id] = {borrowRequestId}
                """)
            .FirstOrDefaultAsync();
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

    private static bool
        IsDuplicatePendingBorrowRequestViolation(
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

            var isPendingRequestIndex =
                error.Message.Contains(
                    "UX_BorrowRequests_UserId_BookId_Pending",
                    StringComparison
                        .OrdinalIgnoreCase);

            if (
                isDuplicateError &&
                isPendingRequestIndex)
            {
                return true;
            }
        }

        return false;
    }
}
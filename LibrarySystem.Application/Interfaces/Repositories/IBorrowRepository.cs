using LibrarySystem.Application.Common.Models;
using LibrarySystem.Domain.Entities;

namespace LibrarySystem.Application.Interfaces.Repositories;

public interface IBorrowRepository
{
    Task<Book?> GetBookByIdAsync(
        int bookId);

    Task<BorrowRecord?> GetActiveBorrowAsync(
        string userId,
        int bookId);

    Task<BorrowRecord?> GetActiveBorrowByIdAsync(
        int borrowRecordId);

    Task<int> GetActiveBorrowCountAsync(
        string userId);

    Task<bool> HasOverdueActiveBorrowAsync(
        string userId,
        DateTime currentDate);

    Task<DateTime?> GetActivePenaltyEndDateAsync(
        string userId,
        DateTime currentDate);

    Task<List<BorrowRecord>> GetUserBorrowsAsync(
        string userId);

    Task<List<BorrowRecord>> GetAllBorrowsAsync();

    Task<Dictionary<string, string>>
        GetUserEmailsAsync(
            IEnumerable<string> userIds);

    Task<BorrowRequest?>
        GetPendingBorrowRequestAsync(
            string userId,
            int bookId);

    Task<BorrowRequest?>
        GetPendingBorrowRequestByIdAsync(
            int borrowRequestId);

    Task<List<BorrowRequest>>
        GetPendingBorrowRequestsAsync();

    Task<List<BorrowRequest>>
        GetPendingBorrowRequestsByUserAsync(
            string userId);

    Task<BorrowRequestWriteStatus>
        CreateBorrowRequestAsync(
            BorrowRequest borrowRequest);

    Task<CancelBorrowRequestWriteStatus>
        CancelBorrowRequestAsync(
            string userId,
            int borrowRequestId,
            DateTime cancelledAt);

    Task<ApproveBorrowRequestWriteResult>
        ApproveBorrowRequestAsync(
            int borrowRequestId,
            string adminUserId,
            DateTime approvalDate);

    Task<RejectBorrowRequestWriteStatus>
        RejectBorrowRequestAsync(
            int borrowRequestId,
            string adminUserId,
            DateTime processedAt,
            string? rejectionReason);

    Task<ReturnRequestWriteStatus>
        RequestReturnAsync(
            string userId,
            int bookId,
            DateTime requestDate);

    Task<BorrowWriteStatus>
        BorrowBookAsync(
            BorrowRecord borrowRecord);

    Task<ReturnBookWriteResult>
        ReturnBookAsync(
            string userId,
            int bookId,
            string adminUserId,
            DateTime returnDate);
}
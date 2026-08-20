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

    Task<BorrowWriteStatus>
        BorrowBookAsync(
            BorrowRecord borrowRecord);

    Task<ReturnBookWriteResult>
        ReturnBookAsync(
            string userId,
            int bookId,
            DateTime returnDate);
}
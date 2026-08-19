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

    Task<List<BorrowRecord>> GetUserBorrowsAsync(
        string userId);

    Task<List<BorrowRecord>> GetAllBorrowsAsync();

    Task<Dictionary<string, string>>
        GetUserEmailsAsync(
            IEnumerable<string> userIds);

    Task<BorrowWriteStatus>
        BorrowBookAsync(
            BorrowRecord borrowRecord);

    Task<ReturnWriteStatus>
        ReturnBookAsync(
            string userId,
            int bookId,
            DateTime returnDate);
}
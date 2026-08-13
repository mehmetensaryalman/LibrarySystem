using LibrarySystem.Domain.Entities;

namespace LibrarySystem.Application.Interfaces.Repositories;

public interface IBorrowRepository
{
    Task<Book?> GetBookByIdAsync(int bookId);

    Task<BorrowRecord?> GetActiveBorrowAsync(
        string userId,
        int bookId);

    Task<List<BorrowRecord>> GetUserBorrowsAsync(
        string userId);

    Task<List<BorrowRecord>> GetAllBorrowsAsync();

    Task<Dictionary<string, string>> GetUserEmailsAsync(
        IEnumerable<string> userIds);

    Task AddBorrowAsync(
        BorrowRecord borrowRecord);

    Task SaveChangesAsync();
}
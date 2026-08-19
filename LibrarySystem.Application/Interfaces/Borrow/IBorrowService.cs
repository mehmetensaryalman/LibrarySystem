using LibrarySystem.Application.DTOs.Borrow;

namespace LibrarySystem.Application.Interfaces.Borrow;

public interface IBorrowService
{
    Task<OperationResultDto> BorrowAsync(
        string userId,
        int bookId);

    Task<OperationResultDto> ReturnAsync(
        string userId,
        int bookId);

    Task<OperationResultDto>
        ReturnForAdminAsync(
            int borrowRecordId);

    Task<List<BorrowedBookResponseDto>>
        GetMyBooksAsync(
            string userId);

    Task<List<AdminBorrowResponseDto>>
        GetAllBorrowsForAdminAsync();
}
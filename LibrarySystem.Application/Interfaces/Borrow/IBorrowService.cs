using LibrarySystem.Application.DTOs.Borrow;

namespace LibrarySystem.Application.Interfaces.Borrow;

public interface IBorrowService
{
    Task<OperationResultDto> BorrowAsync(
        string userId,
        int bookId);

    Task<OperationResultDto>
        CancelBorrowRequestAsync(
            string userId,
            int borrowRequestId);

    Task<OperationResultDto>
        RequestReturnAsync(
            string userId,
            int bookId);

    Task<OperationResultDto>
        ReturnForAdminAsync(
            int borrowRecordId,
            string adminUserId);

    Task<List<BorrowedBookResponseDto>>
        GetMyBooksAsync(
            string userId);

    Task<BorrowPenaltyStatusDto>
        GetMyPenaltyStatusAsync(
            string userId);

    Task<List<AdminBorrowResponseDto>>
        GetAllBorrowsForAdminAsync();

    Task<List<AdminBorrowRequestResponseDto>>
        GetPendingBorrowRequestsForAdminAsync();

    Task<List<MyBorrowRequestResponseDto>>
        GetMyPendingBorrowRequestsAsync(
            string userId);

    Task<OperationResultDto>
        ApproveBorrowRequestAsync(
            int borrowRequestId,
            string adminUserId);

    Task<OperationResultDto>
        RejectBorrowRequestAsync(
            int borrowRequestId,
            string adminUserId,
            string? rejectionReason);
}
using LibrarySystem.Application.Common.Constants;
using LibrarySystem.Application.Common.Models;
using LibrarySystem.Application.DTOs.Borrow;
using LibrarySystem.Application.DTOs.Notifications;
using LibrarySystem.Application.Interfaces.Borrow;
using LibrarySystem.Application.Interfaces.Notifications;
using LibrarySystem.Application.Interfaces.Realtime;
using LibrarySystem.Application.Interfaces.Repositories;
using LibrarySystem.Domain.Entities;
using LibrarySystem.Domain.Enums;

namespace LibrarySystem.Application.Services.Borrow;

public class BorrowService :
    IBorrowService
{
    private readonly IBorrowRepository _borrowRepository;
    private readonly INotificationService _notificationService;
    private readonly IRealtimeNotifier _realtimeNotifier;

    public BorrowService(
        IBorrowRepository borrowRepository,
        INotificationService notificationService,
        IRealtimeNotifier realtimeNotifier)
    {
        _borrowRepository = borrowRepository;
        _notificationService = notificationService;
        _realtimeNotifier = realtimeNotifier;
    }

    public async Task<OperationResultDto> BorrowAsync(
        string userId,
        int bookId)
    {
        var book =
            await _borrowRepository.GetBookByIdAsync(
                bookId);

        if (book is null)
        {
            return new OperationResultDto
            {
                Success = false,
                Message = "Kitap bulunamadı."
            };
        }

        var requestDate =
            DateTime.UtcNow;

        var hasOverdueBorrow =
            await _borrowRepository
                .HasOverdueActiveBorrowAsync(
                    userId,
                    requestDate);

        if (hasOverdueBorrow)
        {
            return new OperationResultDto
            {
                Success = false,
                Message =
                    "Gecikmiş kitabınız bulunduğu için yeni ödünç talebi oluşturamazsınız. Önce geciken kitabınızı fiziksel olarak kütüphaneye teslim etmeniz gerekmektedir."
            };
        }

        var activePenaltyEndDate =
            await _borrowRepository
                .GetActivePenaltyEndDateAsync(
                    userId,
                    requestDate);

        if (activePenaltyEndDate.HasValue)
        {
            return new OperationResultDto
            {
                Success = false,
                Message =
                    "Aktif cezanız bulunduğu için yeni ödünç talebi oluşturamazsınız.",
                PenaltyEndDate =
                    AsUtc(activePenaltyEndDate)
            };
        }

        var activeBorrow =
            await _borrowRepository
                .GetActiveBorrowAsync(
                    userId,
                    bookId);

        if (activeBorrow is not null)
        {
            return new OperationResultDto
            {
                Success = false,
                Message =
                    "Bu kitap zaten aktif olarak üzerinizde bulunmaktadır."
            };
        }

        var activeBorrowCount =
            await _borrowRepository
                .GetActiveBorrowCountAsync(
                    userId);

        if (
            activeBorrowCount >=
            BorrowRules.MaxActiveBorrowCount)
        {
            return new OperationResultDto
            {
                Success = false,
                Message =
                    $"Aynı anda en fazla {BorrowRules.MaxActiveBorrowCount} kitap ödünç alabilirsiniz. Yeni ödünç talebi oluşturabilmek için mevcut kitaplarınızdan en az birini fiziksel olarak iade etmeniz gerekmektedir."
            };
        }

        var pendingRequest =
            await _borrowRepository
                .GetPendingBorrowRequestAsync(
                    userId,
                    bookId);

        if (pendingRequest is not null)
        {
            return new OperationResultDto
            {
                Success = false,
                Message =
                    "Bu kitap için zaten bekleyen bir ödünç talebiniz bulunmaktadır."
            };
        }

        if (book.Stock <= 0)
        {
            return new OperationResultDto
            {
                Success = false,
                Message =
                    "Kitap şu anda stokta bulunmadığı için ödünç talebi oluşturulamıyor."
            };
        }

        var borrowRequest =
            new BorrowRequest
            {
                UserId = userId,
                BookId = bookId,
                Status =
                    BorrowRequestStatus.Pending,
                RequestedAt = requestDate,
                ProcessedAt = null,
                ProcessedByAdminUserId = null,
                BorrowRecordId = null,
                RejectionReason = null
            };

        var writeStatus =
            await _borrowRepository
                .CreateBorrowRequestAsync(
                    borrowRequest);

        if (
            writeStatus ==
            BorrowRequestWriteStatus
                .DuplicatePendingRequest)
        {
            return new OperationResultDto
            {
                Success = false,
                Message =
                    "Bu kitap için zaten bekleyen bir ödünç talebiniz bulunmaktadır."
            };
        }

        var userEmails =
            await _borrowRepository
                .GetUserEmailsAsync(
                    new[]
                    {
                        userId
                    });

        var userEmail =
            userEmails.TryGetValue(
                userId,
                out var email)
                ? email
                : "Bilinmiyor";

        await _notificationService
            .CreateForAdminsAsync(
                new CreateAdminNotificationDto
                {
                    Type =
                        NotificationType
                            .BorrowRequested,

                    Title =
                        "Yeni Ödünç Talebi",

                    Message =
                        $"{userEmail} kullanıcısı \"{book.Name}\" kitabı için ödünç talebi oluşturdu.",

                    BorrowRecordId =
                        null
                });

        await _realtimeNotifier
            .NotifyAdminNotificationsChangedAsync();

        return new OperationResultDto
        {
            Success = true,
            Message =
                "Ödünç talebiniz başarıyla oluşturuldu. Kitabı teslim almak için kütüphane görevlisinin onayı gerekmektedir."
        };
    }

    public async Task<
    List<MyBorrowRequestResponseDto>>
    GetMyPendingBorrowRequestsAsync(
        string userId)
    {
        var requests =
            await _borrowRepository
                .GetPendingBorrowRequestsByUserAsync(
                    userId);

        return requests
            .Select(request =>
                new MyBorrowRequestResponseDto
                {
                    BorrowRequestId =
                        request.Id,

                    BookId =
                        request.BookId,

                    BookName =
                        request.Book.Name,

                    Author =
                        request.Book.Author,

                    RequestedAt =
                        AsUtc(
                            request.RequestedAt)
                })
            .ToList();
    }

    public async Task<
        List<AdminBorrowRequestResponseDto>>
        GetPendingBorrowRequestsForAdminAsync()
    {
        var requests =
            await _borrowRepository
                .GetPendingBorrowRequestsAsync();

        var userEmails =
            await _borrowRepository
                .GetUserEmailsAsync(
                    requests.Select(
                        request =>
                            request.UserId));

        return requests
            .Select(request =>
            {
                var userEmail =
                    userEmails.TryGetValue(
                        request.UserId,
                        out var email)
                        ? email
                        : "Bilinmiyor";

                return new AdminBorrowRequestResponseDto
                {
                    BorrowRequestId =
                        request.Id,

                    UserId =
                        request.UserId,

                    UserEmail =
                        userEmail,

                    BookId =
                        request.BookId,

                    BookName =
                        request.Book.Name,

                    Author =
                        request.Book.Author,

                    RequestedAt =
                        AsUtc(
                            request.RequestedAt)
                };
            })
            .ToList();
    }

    public async Task<OperationResultDto>
        ApproveBorrowRequestAsync(
            int borrowRequestId,
            string adminUserId)
    {
        var result =
            await _borrowRepository
                .ApproveBorrowRequestAsync(
                    borrowRequestId,
                    adminUserId,
                    DateTime.UtcNow);

        switch (result.Status)
        {
            case
                ApproveBorrowRequestWriteStatus
                    .PendingRequestNotFound:

                return new OperationResultDto
                {
                    Success = false,
                    Message =
                        "Bekleyen ödünç talebi bulunamadı veya talep daha önce işlenmiş."
                };

            case
                ApproveBorrowRequestWriteStatus
                    .BookUnavailable:

                return new OperationResultDto
                {
                    Success = false,
                    Message =
                        "Kitap artık stokta bulunmuyor veya arşivlenmiş. Ödünç talebi onaylanamadı."
                };

            case
                ApproveBorrowRequestWriteStatus
                    .DuplicateActiveBorrow:

                return new OperationResultDto
                {
                    Success = false,
                    Message =
                        "Kullanıcı bu kitabı zaten aktif olarak ödünç almış durumda."
                };

            case
                ApproveBorrowRequestWriteStatus
                    .OverdueActiveBorrow:

                return new OperationResultDto
                {
                    Success = false,
                    Message =
                        "Kullanıcının gecikmiş kitabı bulunduğu için ödünç talebi onaylanamaz."
                };

            case
                ApproveBorrowRequestWriteStatus
                    .ActivePenalty:

                var penaltyEndDate =
                    !string.IsNullOrWhiteSpace(
                        result.UserId)
                        ? await _borrowRepository
                            .GetActivePenaltyEndDateAsync(
                                result.UserId,
                                DateTime.UtcNow)
                        : null;

                return new OperationResultDto
                {
                    Success = false,
                    Message =
                        "Kullanıcının aktif ödünç alma cezası bulunduğu için talep onaylanamaz.",
                    PenaltyEndDate =
                        AsUtc(
                            penaltyEndDate)
                };

            case
                ApproveBorrowRequestWriteStatus
                    .ActiveBorrowLimitReached:

                return new OperationResultDto
                {
                    Success = false,
                    Message =
                        $"Kullanıcı aynı anda en fazla {BorrowRules.MaxActiveBorrowCount} kitap ödünç alabilir. Aktif ödünç limiti dolu."
                };

            case
                ApproveBorrowRequestWriteStatus
                    .Success:

                await _realtimeNotifier
                    .NotifyBooksChangedAsync();

                await _realtimeNotifier
                    .NotifyBorrowsChangedAsync();

                return new OperationResultDto
                {
                    Success = true,
                    Message =
                        "Ödünç talebi onaylandı. Kitap kullanıcıya fiziksel olarak teslim edildi ve ödünç kaydı oluşturuldu."
                };

            default:

                return new OperationResultDto
                {
                    Success = false,
                    Message =
                        "Ödünç talebi işlenirken beklenmeyen bir durum oluştu."
                };
        }
    }

    public async Task<OperationResultDto>
        RejectBorrowRequestAsync(
            int borrowRequestId,
            string adminUserId,
            string? rejectionReason)
    {
        var normalizedReason =
            string.IsNullOrWhiteSpace(
                rejectionReason)
                ? null
                : rejectionReason.Trim();

        if (
            normalizedReason is not null &&
            normalizedReason.Length > 500)
        {
            return new OperationResultDto
            {
                Success = false,
                Message =
                    "Reddetme açıklaması en fazla 500 karakter olabilir."
            };
        }

        var result =
            await _borrowRepository
                .RejectBorrowRequestAsync(
                    borrowRequestId,
                    adminUserId,
                    DateTime.UtcNow,
                    normalizedReason);

        if (
            result ==
            RejectBorrowRequestWriteStatus
                .PendingRequestNotFound)
        {
            return new OperationResultDto
            {
                Success = false,
                Message =
                    "Bekleyen ödünç talebi bulunamadı veya talep daha önce işlenmiş."
            };
        }

        return new OperationResultDto
        {
            Success = true,
            Message =
                "Ödünç talebi reddedildi."
        };
    }

    public async Task<OperationResultDto>
        RequestReturnAsync(
            string userId,
            int bookId)
    {
        var activeBorrow =
            await _borrowRepository
                .GetActiveBorrowAsync(
                    userId,
                    bookId);

        if (activeBorrow is null)
        {
            return new OperationResultDto
            {
                Success = false,
                Message =
                    "İade talebi oluşturulabilecek aktif ödünç kaydı bulunamadı."
            };
        }

        if (
            activeBorrow.ReturnRequestedAt
                .HasValue)
        {
            return new OperationResultDto
            {
                Success = false,
                Message =
                    "Bu kitap için zaten bekleyen bir iade talebiniz bulunmaktadır."
            };
        }

        var book =
            await _borrowRepository
                .GetBookByIdAsync(
                    bookId);

        if (book is null)
        {
            return new OperationResultDto
            {
                Success = false,
                Message =
                    "Kitap bulunamadı."
            };
        }

        var writeStatus =
            await _borrowRepository
                .RequestReturnAsync(
                    userId,
                    bookId,
                    DateTime.UtcNow);

        if (
            writeStatus ==
            ReturnRequestWriteStatus
                .ActiveBorrowNotFound)
        {
            return new OperationResultDto
            {
                Success = false,
                Message =
                    "İade talebi oluşturulabilecek aktif ödünç kaydı bulunamadı."
            };
        }

        if (
            writeStatus ==
            ReturnRequestWriteStatus
                .AlreadyRequested)
        {
            return new OperationResultDto
            {
                Success = false,
                Message =
                    "Bu kitap için zaten bekleyen bir iade talebiniz bulunmaktadır."
            };
        }

        var userEmails =
            await _borrowRepository
                .GetUserEmailsAsync(
                    new[]
                    {
                        userId
                    });

        var userEmail =
            userEmails.TryGetValue(
                userId,
                out var email)
                ? email
                : "Bilinmiyor";

        await _notificationService
            .CreateForAdminsAsync(
                new CreateAdminNotificationDto
                {
                    Type =
                        NotificationType
                            .ReturnRequested,

                    Title =
                        "Yeni İade Talebi",

                    Message =
                        $"{userEmail} kullanıcısı \"{book.Name}\" kitabı için iade talebi oluşturdu.",

                    BorrowRecordId =
                        activeBorrow.Id
                });

        await _realtimeNotifier
            .NotifyAdminNotificationsChangedAsync();

        await _realtimeNotifier
            .NotifyBorrowsChangedAsync();

        return new OperationResultDto
        {
            Success = true,
            Message =
                "İade talebiniz başarıyla oluşturuldu. Kitabı kütüphane görevlisine fiziksel olarak teslim ediniz. İade işlemi görevli onayından sonra tamamlanacaktır."
        };
    }

    public async Task<OperationResultDto>
        ReturnForAdminAsync(
            int borrowRecordId,
            string adminUserId)
    {
        var activeBorrow =
            await _borrowRepository
                .GetActiveBorrowByIdAsync(
                    borrowRecordId);

        if (activeBorrow is null)
        {
            return new OperationResultDto
            {
                Success = false,
                Message =
                    "İade edilecek aktif ödünç kaydı bulunamadı."
            };
        }

        var writeResult =
            await _borrowRepository
                .ReturnBookAsync(
                    activeBorrow.UserId,
                    activeBorrow.BookId,
                    adminUserId,
                    DateTime.UtcNow);

        if (
            writeResult.Status ==
            ReturnWriteStatus
                .ActiveBorrowNotFound)
        {
            return new OperationResultDto
            {
                Success = false,
                Message =
                    "İade edilecek aktif ödünç kaydı bulunamadı."
            };
        }

        if (
            writeResult.Status ==
            ReturnWriteStatus
                .BookNotFound)
        {
            return new OperationResultDto
            {
                Success = false,
                Message =
                    "Kitap bulunamadı."
            };
        }

        await _realtimeNotifier
            .NotifyBooksChangedAsync();

        await _realtimeNotifier
            .NotifyBorrowsChangedAsync();

        if (writeResult.PenaltyDays > 0)
        {
            return new OperationResultDto
            {
                Success = true,
                Message =
                    $"Kitap fiziksel olarak teslim alındı ve iade işlemi tamamlandı. Kullanıcıya {writeResult.PenaltyDays} günlük ödünç alma cezası uygulandı."
            };
        }

        return new OperationResultDto
        {
            Success = true,
            Message =
                "Kitap fiziksel olarak teslim alındı ve iade işlemi başarıyla tamamlandı."
        };
    }

    public async Task<
        List<BorrowedBookResponseDto>>
        GetMyBooksAsync(
            string userId)
    {
        var borrowRecords =
            await _borrowRepository
                .GetUserBorrowsAsync(
                    userId);

        return borrowRecords
            .Select(record =>
                new BorrowedBookResponseDto
                {
                    BorrowRecordId =
                        record.Id,

                    BookId =
                        record.BookId,

                    BookName =
                        record.Book.Name,

                    Author =
                        record.Book.Author,

                    BorrowDate =
                        AsUtc(
                            record.BorrowDate),

                    DueDate =
                        AsUtc(
                            record.DueDate),

                    ReturnRequestedAt =
                        AsUtc(
                            record.ReturnRequestedAt),

                    ReturnDate =
                        AsUtc(
                            record.ReturnDate),

                    IsReturned =
                        record.IsReturned
                })
            .ToList();
    }

    public async Task<BorrowPenaltyStatusDto>
        GetMyPenaltyStatusAsync(
            string userId)
    {
        var currentDate =
            DateTime.UtcNow;

        var hasOverdueBorrow =
            await _borrowRepository
                .HasOverdueActiveBorrowAsync(
                    userId,
                    currentDate);

        var activePenaltyEndDate =
            await _borrowRepository
                .GetActivePenaltyEndDateAsync(
                    userId,
                    currentDate);

        return new BorrowPenaltyStatusDto
        {
            HasOverdueBorrow =
                hasOverdueBorrow,

            HasActivePenalty =
                activePenaltyEndDate
                    .HasValue,

            PenaltyEndDate =
                AsUtc(
                    activePenaltyEndDate)
        };
    }

    public async Task<
        List<AdminBorrowResponseDto>>
        GetAllBorrowsForAdminAsync()
    {
        var borrowRecords =
            await _borrowRepository
                .GetAllBorrowsAsync();

        var userEmails =
            await _borrowRepository
                .GetUserEmailsAsync(
                    borrowRecords
                        .Select(record =>
                            record.UserId));

        return borrowRecords
            .Select(record =>
            {
                var userEmail =
                    userEmails.TryGetValue(
                        record.UserId,
                        out var email)
                        ? email
                        : "Bilinmiyor";

                return new AdminBorrowResponseDto
                {
                    BorrowRecordId =
                        record.Id,

                    UserId =
                        record.UserId,

                    UserEmail =
                        userEmail,

                    BookId =
                        record.BookId,

                    BookName =
                        record.Book.Name,

                    Author =
                        record.Book.Author,

                    BorrowDate =
                        AsUtc(
                            record.BorrowDate),

                    DueDate =
                        AsUtc(
                            record.DueDate),

                    ReturnRequestedAt =
                        AsUtc(
                            record.ReturnRequestedAt),

                    ReturnDate =
                        AsUtc(
                            record.ReturnDate),

                    ReturnedToAdminUserId =
                        record.ReturnedToAdminUserId,

                    IsReturned =
                        record.IsReturned
                };
            })
            .ToList();
    }

    private static DateTime AsUtc(
        DateTime dateTime)
    {
        return DateTime.SpecifyKind(
            dateTime,
            DateTimeKind.Utc);
    }

    private static DateTime? AsUtc(
        DateTime? dateTime)
    {
        if (!dateTime.HasValue)
        {
            return null;
        }

        return DateTime.SpecifyKind(
            dateTime.Value,
            DateTimeKind.Utc);
    }
}
using LibrarySystem.Application.Common.Constants;
using LibrarySystem.Application.Common.Models;
using LibrarySystem.Application.DTOs.Borrow;
using LibrarySystem.Application.Interfaces.Borrow;
using LibrarySystem.Application.Interfaces.Realtime;
using LibrarySystem.Application.Interfaces.Repositories;
using LibrarySystem.Domain.Entities;

namespace LibrarySystem.Application.Services.Borrow;

public class BorrowService :
    IBorrowService
{
    private const int
        BorrowDurationDays = 7;

    private readonly
        IBorrowRepository _borrowRepository;

    private readonly
        IRealtimeNotifier _realtimeNotifier;

    public BorrowService(
        IBorrowRepository borrowRepository,
        IRealtimeNotifier realtimeNotifier)
    {
        _borrowRepository =
            borrowRepository;

        _realtimeNotifier =
            realtimeNotifier;
    }

    public async Task<OperationResultDto>
        BorrowAsync(
            string userId,
            int bookId)
    {
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

        var borrowDate =
            DateTime.UtcNow;

        var hasOverdueBorrow =
            await _borrowRepository
                .HasOverdueActiveBorrowAsync(
                    userId,
                    borrowDate);

        if (hasOverdueBorrow)
        {
            return new OperationResultDto
            {
                Success = false,

                Message =
                    "Gecikmiş kitabınız bulunduğu için yeni kitap ödünç alamazsınız. Önce geciken kitabınızı iade etmeniz gerekmektedir."
            };
        }

        var activePenaltyEndDate =
            await _borrowRepository
                .GetActivePenaltyEndDateAsync(
                    userId,
                    borrowDate);

        if (activePenaltyEndDate.HasValue)
        {
            return new OperationResultDto
            {
                Success = false,

                Message =
                    "Aktif cezanız bulunduğu için yeni kitap ödünç alamazsınız."
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
                    "Bu kitabı iade etmeden tekrar ödünç alamazsınız."
            };
        }

        var activeBorrowCount =
            await _borrowRepository
                .GetActiveBorrowCountAsync(
                    userId);

        if (
            activeBorrowCount >=
            BorrowRules
                .MaxActiveBorrowCount)
        {
            return new OperationResultDto
            {
                Success = false,

                Message =
                    $"Aynı anda en fazla {BorrowRules.MaxActiveBorrowCount} kitap ödünç alabilirsiniz. Yeni kitap ödünç alabilmek için mevcut kitaplarınızdan en az birini iade etmeniz gerekmektedir."
            };
        }

        if (book.Stock <= 0)
        {
            return new OperationResultDto
            {
                Success = false,

                Message =
                    "Kitap stokta bulunmuyor."
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

        var borrowRecord =
            new BorrowRecord
            {
                UserId =
                    userId,

                BookId =
                    bookId,

                BorrowDate =
                    borrowDate,

                DueDate =
                    borrowDate.AddDays(
                        BorrowDurationDays),

                IsReturned =
                    false
            };

        var writeStatus =
            await _borrowRepository
                .BorrowBookAsync(
                    borrowRecord);

        if (
            writeStatus ==
            BorrowWriteStatus
                .OverdueActiveBorrow)
        {
            return new OperationResultDto
            {
                Success = false,

                Message =
                    "Gecikmiş kitabınız bulunduğu için yeni kitap ödünç alamazsınız. Önce geciken kitabınızı iade etmeniz gerekmektedir."
            };
        }

        if (
            writeStatus ==
            BorrowWriteStatus
                .ActivePenalty)
        {
            return new OperationResultDto
            {
                Success = false,

                Message =
                    "Aktif cezanız bulunduğu için yeni kitap ödünç alamazsınız."
            };
        }

        if (
            writeStatus ==
            BorrowWriteStatus
                .ActiveBorrowLimitReached)
        {
            return new OperationResultDto
            {
                Success = false,

                Message =
                    $"Aynı anda en fazla {BorrowRules.MaxActiveBorrowCount} kitap ödünç alabilirsiniz. Yeni kitap ödünç alabilmek için mevcut kitaplarınızdan en az birini iade etmeniz gerekmektedir."
            };
        }

        if (
            writeStatus ==
            BorrowWriteStatus
                .DuplicateActiveBorrow)
        {
            return new OperationResultDto
            {
                Success = false,

                Message =
                    "Bu kitabı iade etmeden tekrar ödünç alamazsınız."
            };
        }

        if (
            writeStatus ==
            BorrowWriteStatus
                .BookUnavailable)
        {
            var currentBook =
                await _borrowRepository
                    .GetBookByIdAsync(
                        bookId);

            if (currentBook is null)
            {
                return new OperationResultDto
                {
                    Success = false,

                    Message =
                        "Kitap bulunamadı."
                };
            }

            return new OperationResultDto
            {
                Success = false,

                Message =
                    "Kitap stokta bulunmuyor."
            };
        }

        await _realtimeNotifier
            .NotifyBooksChangedAsync();

        await _realtimeNotifier
            .NotifyBorrowsChangedAsync();

        await _realtimeNotifier
            .NotifyAdminBorrowNotificationAsync(
                new AdminBorrowNotificationDto
                {
                    BookId =
                        book.Id,

                    BookName =
                        book.Name,

                    UserEmail =
                        userEmail,

                    BorrowDate =
                        borrowDate
                });

        return new OperationResultDto
        {
            Success = true,

            Message =
                "Kitap başarıyla ödünç alındı."
        };
    }

    public async Task<OperationResultDto>
        ReturnAsync(
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
                    "İade edilecek aktif ödünç kaydı bulunamadı."
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

        var writeResult =
            await _borrowRepository
                .ReturnBookAsync(
                    userId,
                    bookId,
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
                    $"Kitap başarıyla iade edildi. {writeResult.PenaltyDays} günlük ödünç alma cezası uygulandı."
            };
        }

        return new OperationResultDto
        {
            Success = true,

            Message =
                "Kitap başarıyla iade edildi."
        };
    }

    public async Task<OperationResultDto>
        ReturnForAdminAsync(
            int borrowRecordId)
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
                    $"Kitap başarıyla iade alındı. Kullanıcıya {writeResult.PenaltyDays} günlük ödünç alma cezası uygulandı."
            };
        }

        return new OperationResultDto
        {
            Success = true,

            Message =
                "Kitap başarıyla iade alındı."
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

                    ReturnDate =
                        AsUtc(
                            record.ReturnDate),

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
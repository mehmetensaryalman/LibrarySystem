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

        if (book.Stock <= 0)
        {
            return new OperationResultDto
            {
                Success = false,

                Message =
                    "Kitap stokta bulunmuyor."
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

        var borrowDate =
            DateTime.UtcNow;

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

        var writeStatus =
            await _borrowRepository
                .ReturnBookAsync(
                    userId,
                    bookId,
                    DateTime.UtcNow);

        if (
            writeStatus ==
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
            writeStatus ==
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

        var writeStatus =
            await _borrowRepository
                .ReturnBookAsync(
                    activeBorrow.UserId,
                    activeBorrow.BookId,
                    DateTime.UtcNow);

        if (
            writeStatus ==
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
            writeStatus ==
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
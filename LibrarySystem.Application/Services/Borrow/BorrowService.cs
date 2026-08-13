using LibrarySystem.Application.DTOs.Borrow;
using LibrarySystem.Application.Interfaces.Borrow;
using LibrarySystem.Application.Interfaces.Repositories;
using LibrarySystem.Domain.Entities;

namespace LibrarySystem.Application.Services.Borrow;

public class BorrowService : IBorrowService
{
    private const int BorrowDurationDays = 7;

    private readonly IBorrowRepository _borrowRepository;

    public BorrowService(
        IBorrowRepository borrowRepository)
    {
        _borrowRepository = borrowRepository;
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

        if (book.Stock <= 0)
        {
            return new OperationResultDto
            {
                Success = false,
                Message = "Kitap stokta bulunmuyor."
            };
        }

        var activeBorrow =
            await _borrowRepository.GetActiveBorrowAsync(
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

        var borrowDate = DateTime.UtcNow;

        var borrowRecord =
            new BorrowRecord
            {
                UserId = userId,
                BookId = bookId,
                BorrowDate = borrowDate,
                DueDate =
                    borrowDate.AddDays(
                        BorrowDurationDays),
                IsReturned = false
            };

        book.Stock--;

        await _borrowRepository
            .AddBorrowAsync(borrowRecord);

        await _borrowRepository
            .SaveChangesAsync();

        return new OperationResultDto
        {
            Success = true,
            Message =
                "Kitap başarıyla ödünç alındı."
        };
    }

    public async Task<OperationResultDto> ReturnAsync(
        string userId,
        int bookId)
    {
        var activeBorrow =
            await _borrowRepository.GetActiveBorrowAsync(
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

        activeBorrow.IsReturned = true;
        activeBorrow.ReturnDate = DateTime.UtcNow;

        book.Stock++;

        await _borrowRepository
            .SaveChangesAsync();

        return new OperationResultDto
        {
            Success = true,
            Message =
                "Kitap başarıyla iade edildi."
        };
    }

    public async Task<List<BorrowedBookResponseDto>>
        GetMyBooksAsync(
            string userId)
    {
        var borrowRecords =
            await _borrowRepository
                .GetUserBorrowsAsync(userId);

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
                        AsUtc(record.BorrowDate),

                    DueDate =
                        AsUtc(record.DueDate),

                    ReturnDate =
                        AsUtc(record.ReturnDate),

                    IsReturned =
                        record.IsReturned
                })
            .ToList();
    }

    public async Task<List<AdminBorrowResponseDto>>
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
                        AsUtc(record.BorrowDate),

                    DueDate =
                        AsUtc(record.DueDate),

                    ReturnDate =
                        AsUtc(record.ReturnDate),

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
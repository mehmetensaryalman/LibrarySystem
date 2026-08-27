using LibrarySystem.Application.Common.Exceptions;
using LibrarySystem.Application.Common.Models;
using LibrarySystem.Application.DTOs.Books;
using LibrarySystem.Application.Interfaces.Books;
using LibrarySystem.Application.Interfaces.Realtime;
using LibrarySystem.Application.Interfaces.Repositories;
using LibrarySystem.Domain.Entities;

namespace LibrarySystem.Application.Services.Books;

public class BookService : IBookService
{
    private readonly
        IBookRepository _bookRepository;

    private readonly
        IRealtimeNotifier _realtimeNotifier;

    private readonly
        IBookMetadataProvider
            _bookMetadataProvider;

    public BookService(
        IBookRepository bookRepository,
        IRealtimeNotifier realtimeNotifier,
        IBookMetadataProvider
            bookMetadataProvider)
    {
        _bookRepository =
            bookRepository;

        _realtimeNotifier =
            realtimeNotifier;

        _bookMetadataProvider =
            bookMetadataProvider;
    }

    public async Task<List<BookResponseDto>>
        GetAllAsync()
    {
        var books =
            await _bookRepository
                .GetAllAsync();

        return books
            .Select(book =>
                new BookResponseDto
                {
                    Id = book.Id,
                    Name = book.Name,
                    Author = book.Author,
                    Stock = book.Stock
                })
            .ToList();
    }

    public async Task<
        PagedResult<BookResponseDto>>
        GetPagedAsync(
            BookFilterRequestDto request)
    {
        var search =
            string.IsNullOrWhiteSpace(
                request.Search)
                ? null
                : request.Search.Trim();

        var result =
            await _bookRepository
                .GetPagedAsync(
                    search,
                    request.InStock,
                    request.SortBy,
                    request.PageNumber,
                    request.PageSize);

        return new PagedResult<
            BookResponseDto>
        {
            Items =
                result.Items
                    .Select(book =>
                        new BookResponseDto
                        {
                            Id = book.Id,
                            Name = book.Name,
                            Author =
                                book.Author,
                            Stock =
                                book.Stock
                        })
                    .ToList(),

            PageNumber =
                result.PageNumber,

            PageSize =
                result.PageSize,

            TotalCount =
                result.TotalCount
        };
    }

    public async Task<BookPreviewResponseDto?>
        GetPreviewAsync(
            int id,
            CancellationToken
                cancellationToken = default)
    {
        var book =
            await _bookRepository
                .GetByIdAsync(id);

        if (book is null)
        {
            book =
                await _bookRepository
                    .GetArchivedByIdAsync(
                        id);
        }

        if (book is null)
        {
            return null;
        }

        var metadata =
            await _bookMetadataProvider
                .GetMetadataAsync(
                    book.Name,
                    book.Author,
                    cancellationToken);

        return new BookPreviewResponseDto
        {
            Id = book.Id,
            Name = book.Name,
            Author = book.Author,

            CoverImageUrl =
                metadata?.CoverImageUrl,

            PageCount =
                metadata?.PageCount,

            Summary =
                metadata?.Summary,

            InfoUrl =
                metadata?.InfoUrl,

            Source =
                metadata is null
                    ? null
                    : "Google Books",

            MetadataFound =
                metadata is not null
        };
    }

    public async Task<BookResponseDto>
        CreateAsync(
            CreateBookRequestDto request)
    {
        var name =
            request.Name.Trim();

        var author =
            request.Author.Trim();

        var bookAlreadyExists =
            await _bookRepository
                .ExistsByNameAndAuthorAsync(
                    name,
                    author);

        if (bookAlreadyExists)
        {
            throw new DuplicateBookException(
                "Bu kitap ve yazar bilgileriyle kayıtlı bir kitap zaten mevcut.");
        }

        var book = new Book
        {
            Name = name,
            Author = author,
            Stock = request.Stock
        };

        await _bookRepository
            .AddAsync(book);

        await _realtimeNotifier
            .NotifyBooksChangedAsync();

        return new BookResponseDto
        {
            Id = book.Id,
            Name = book.Name,
            Author = book.Author,
            Stock = book.Stock
        };
    }

    public async Task<BookResponseDto?>
        UpdateAsync(
            int id,
            UpdateBookRequestDto request)
    {
        var book =
            await _bookRepository
                .GetByIdAsync(id);

        if (book is null)
        {
            return null;
        }

        var name =
            request.Name.Trim();

        var author =
            request.Author.Trim();

        var duplicateBookExists =
            await _bookRepository
                .ExistsByNameAndAuthorAsync(
                    name,
                    author,
                    id);

        if (duplicateBookExists)
        {
            throw new DuplicateBookException(
                "Bu kitap ve yazar bilgileriyle kayıtlı bir kitap zaten mevcut.");
        }

        book.Name = name;
        book.Author = author;
        book.Stock = request.Stock;

        var updatedBook =
            await _bookRepository
                .UpdateAsync(book);

        await _realtimeNotifier
            .NotifyBooksChangedAsync();

        return new BookResponseDto
        {
            Id = updatedBook.Id,
            Name = updatedBook.Name,
            Author =
                updatedBook.Author,
            Stock =
                updatedBook.Stock
        };
    }

    public async Task<
        List<ArchivedBookResponseDto>>
        GetArchivedAsync()
    {
        var books =
            await _bookRepository
                .GetArchivedAsync();

        return books
            .Select(book =>
                new ArchivedBookResponseDto
                {
                    Id = book.Id,
                    Name = book.Name,
                    Author = book.Author,
                    Stock = book.Stock,
                    IsArchived =
                        book.IsArchived,
                    ArchivedAt =
                        AsUtc(
                            book.ArchivedAt)
                })
            .ToList();
    }

    public async Task<
        ArchivedBookResponseDto?>
        RestoreAsync(
            int id)
    {
        var book =
            await _bookRepository
                .GetArchivedByIdAsync(
                    id);

        if (book is null)
        {
            return null;
        }

        await _bookRepository
            .RestoreAsync(book);

        await _realtimeNotifier
            .NotifyBooksChangedAsync();

        return new ArchivedBookResponseDto
        {
            Id = book.Id,
            Name = book.Name,
            Author = book.Author,
            Stock = book.Stock,
            IsArchived = book.IsArchived,
            ArchivedAt =
                AsUtc(
                    book.ArchivedAt)
        };
    }

    public async Task<DeleteBookResult>
        DeleteAsync(
            int id)
    {
        var book =
            await _bookRepository
                .GetByIdAsync(id);

        if (book is null)
        {
            return new DeleteBookResult
            {
                Status =
                    DeleteBookStatus.NotFound,

                Message =
                    "Kitap bulunamadı."
            };
        }

        var hasActiveBorrow =
            await _bookRepository
                .HasActiveBorrowAsync(id);

        if (hasActiveBorrow)
        {
            return new DeleteBookResult
            {
                Status =
                    DeleteBookStatus
                        .ActiveBorrowExists,

                Message =
                    "Bu kitap şu anda ödünç alındığı için katalogdan kaldırılamaz."
            };
        }

        var hasPendingBorrowRequest =
            await _bookRepository
                .HasPendingBorrowRequestAsync(
                    id);

        if (hasPendingBorrowRequest)
        {
            return new DeleteBookResult
            {
                Status =
                    DeleteBookStatus
                        .ActiveBorrowExists,

                Message =
                    "Bu kitap için bekleyen ödünç talebi bulunduğu için katalogdan kaldırılamaz."
            };
        }

        var hasBorrowHistory =
            await _bookRepository
                .HasBorrowHistoryAsync(id);

        var hasBorrowRequestHistory =
            await _bookRepository
                .HasBorrowRequestHistoryAsync(
                    id);

        if (
            hasBorrowHistory ||
            hasBorrowRequestHistory)
        {
            await _bookRepository
                .ArchiveAsync(book);

            await _realtimeNotifier
                .NotifyBooksChangedAsync();

            return new DeleteBookResult
            {
                Status =
                    DeleteBookStatus.Archived,

                Message =
                    "Kitabın ödünç veya talep geçmişi bulunduğu için kayıt silinmedi, arşivlendi."
            };
        }

        await _bookRepository
            .DeleteAsync(book);

        await _realtimeNotifier
            .NotifyBooksChangedAsync();

        return new DeleteBookResult
        {
            Status =
                DeleteBookStatus.Deleted,

            Message =
                "Kitap başarıyla silindi."
        };
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

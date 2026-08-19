using LibrarySystem.Application.Common.Exceptions;
using LibrarySystem.Application.Common.Models;
using LibrarySystem.Application.Interfaces.Repositories;
using LibrarySystem.Domain.Entities;
using LibrarySystem.Infrastructure.Persistence;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace LibrarySystem.Infrastructure.Repositories;

public class BookRepository : IBookRepository
{
    private readonly
        LibraryDbContext _dbContext;

    public BookRepository(
        LibraryDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<List<Book>>
        GetAllAsync()
    {
        return await _dbContext.Books
            .AsNoTracking()
            .Where(book =>
                !book.IsArchived)
            .OrderByDescending(book =>
                book.Id)
            .ToListAsync();
    }

    public async Task<PagedResult<Book>>
        GetPagedAsync(
            string? search,
            bool? inStock,
            string sortBy,
            int pageNumber,
            int pageSize)
    {
        var query =
            _dbContext.Books
                .AsNoTracking()
                .Where(book =>
                    !book.IsArchived);

        if (
            !string.IsNullOrWhiteSpace(
                search))
        {
            var searchText =
                search.Trim();

            query =
                query.Where(book =>
                    book.Name.Contains(
                        searchText) ||
                    book.Author.Contains(
                        searchText));
        }

        if (inStock.HasValue)
        {
            query =
                inStock.Value
                    ? query.Where(book =>
                        book.Stock > 0)
                    : query.Where(book =>
                        book.Stock == 0);
        }

        var totalCount =
            await query.CountAsync();

        var orderedQuery =
            sortBy switch
            {
                "nameAsc" =>
                    query
                        .OrderBy(book =>
                            book.Name)
                        .ThenBy(book =>
                            book.Author)
                        .ThenByDescending(book =>
                            book.Id),

                _ =>
                    query
                        .OrderByDescending(book =>
                            book.Id)
            };

        var books =
            await orderedQuery
                .Skip(
                    (pageNumber - 1) *
                    pageSize)
                .Take(pageSize)
                .ToListAsync();

        return new PagedResult<Book>
        {
            Items = books,
            PageNumber = pageNumber,
            PageSize = pageSize,
            TotalCount = totalCount
        };
    }

    public async Task<List<Book>>
        GetArchivedAsync()
    {
        return await _dbContext.Books
            .AsNoTracking()
            .Where(book =>
                book.IsArchived)
            .OrderByDescending(book =>
                book.ArchivedAt)
            .ThenBy(book =>
                book.Name)
            .ThenBy(book =>
                book.Author)
            .ThenBy(book =>
                book.Id)
            .ToListAsync();
    }

    public async Task<Book?>
        GetByIdAsync(
            int id)
    {
        return await _dbContext.Books
            .FirstOrDefaultAsync(book =>
                book.Id == id &&
                !book.IsArchived);
    }

    public async Task<Book?>
        GetArchivedByIdAsync(
            int id)
    {
        return await _dbContext.Books
            .FirstOrDefaultAsync(book =>
                book.Id == id &&
                book.IsArchived);
    }

    public async Task<bool>
        ExistsByNameAndAuthorAsync(
            string name,
            string author,
            int? excludedBookId = null)
    {
        var query =
            _dbContext.Books
                .AsNoTracking()
                .Where(book =>
                    book.Name == name &&
                    book.Author == author);

        if (excludedBookId.HasValue)
        {
            query =
                query.Where(book =>
                    book.Id !=
                    excludedBookId.Value);
        }

        return await query.AnyAsync();
    }

    public async Task<Book>
        AddAsync(
            Book book)
    {
        await _dbContext.Books
            .AddAsync(book);

        try
        {
            await _dbContext
                .SaveChangesAsync();
        }
        catch (
            DbUpdateException exception)
            when (
                IsDuplicateBookViolation(
                    exception))
        {
            throw new DuplicateBookException(
                "Bu kitap ve yazar bilgileriyle kayıtlı bir kitap zaten mevcut.",
                exception);
        }

        return book;
    }

    public async Task<Book>
        UpdateAsync(
            Book book)
    {
        try
        {
            await _dbContext
                .SaveChangesAsync();
        }
        catch (
            DbUpdateException exception)
            when (
                IsDuplicateBookViolation(
                    exception))
        {
            throw new DuplicateBookException(
                "Bu kitap ve yazar bilgileriyle kayıtlı bir kitap zaten mevcut.",
                exception);
        }

        return book;
    }

    public async Task
        DeleteAsync(
            Book book)
    {
        _dbContext.Books
            .Remove(book);

        await _dbContext
            .SaveChangesAsync();
    }

    public async Task<bool>
        HasActiveBorrowAsync(
            int bookId)
    {
        return await _dbContext
            .BorrowRecords
            .AnyAsync(record =>
                record.BookId ==
                    bookId &&
                !record.IsReturned);
    }

    public async Task<bool>
        HasBorrowHistoryAsync(
            int bookId)
    {
        return await _dbContext
            .BorrowRecords
            .AnyAsync(record =>
                record.BookId ==
                    bookId);
    }

    public async Task
        ArchiveAsync(
            Book book)
    {
        book.IsArchived = true;

        book.ArchivedAt =
            DateTime.UtcNow;

        await _dbContext
            .SaveChangesAsync();
    }

    public async Task
        RestoreAsync(
            Book book)
    {
        book.IsArchived = false;

        book.ArchivedAt = null;

        await _dbContext
            .SaveChangesAsync();
    }

    private static bool
        IsDuplicateBookViolation(
            DbUpdateException exception)
    {
        if (
            exception.InnerException
            is not SqlException sqlException)
        {
            return false;
        }

        foreach (
            SqlError error
            in sqlException.Errors)
        {
            var isDuplicateError =
                error.Number == 2601 ||
                error.Number == 2627;

            var isBookUniqueIndex =
                error.Message.Contains(
                    "UX_Books_Name_Author",
                    StringComparison
                        .OrdinalIgnoreCase);

            if (
                isDuplicateError &&
                isBookUniqueIndex)
            {
                return true;
            }
        }

        return false;
    }
}
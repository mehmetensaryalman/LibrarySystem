using LibrarySystem.Application.Common.Models;

namespace LibrarySystem.Application.Interfaces.Books;

public interface IBookMetadataProvider
{
    Task<BookMetadataResult?>
        GetMetadataAsync(
            string bookName,
            string author,
            CancellationToken
                cancellationToken = default);
}
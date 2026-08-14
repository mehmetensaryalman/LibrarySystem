namespace LibrarySystem.Api.Hubs;

public interface ILibraryClient
{
    Task BooksChanged();

    Task BorrowsChanged();
}
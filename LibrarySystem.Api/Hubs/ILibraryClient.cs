using LibrarySystem.Application.DTOs.Borrow;

namespace LibrarySystem.Api.Hubs;

public interface ILibraryClient
{
    Task BooksChanged();

    Task BorrowsChanged();

    Task AdminBorrowNotification(
        AdminBorrowNotificationDto notification);
}
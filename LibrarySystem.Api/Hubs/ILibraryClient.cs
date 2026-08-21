using LibrarySystem.Application.DTOs.Borrow;

namespace LibrarySystem.Api.Hubs;

public interface ILibraryClient
{
    Task BooksChanged();

    Task BorrowsChanged();

    Task AdminNotificationsChanged();

    Task AdminBorrowNotification(
        AdminBorrowNotificationDto notification);
}
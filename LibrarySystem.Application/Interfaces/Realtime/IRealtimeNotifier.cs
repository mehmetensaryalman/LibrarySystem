using LibrarySystem.Application.DTOs.Borrow;

namespace LibrarySystem.Application.Interfaces.Realtime;

public interface IRealtimeNotifier
{
    Task NotifyBooksChangedAsync();

    Task NotifyBorrowsChangedAsync();

    Task NotifyAdminNotificationsChangedAsync();

    Task NotifyAdminBorrowNotificationAsync(
        AdminBorrowNotificationDto notification);
}
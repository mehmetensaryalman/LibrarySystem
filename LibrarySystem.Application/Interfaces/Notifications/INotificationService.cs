using LibrarySystem.Application.DTOs.Notifications;

namespace LibrarySystem.Application.Interfaces.Notifications;

public interface INotificationService
{
    Task<NotificationSummaryDto>
        GetMySummaryAsync(
            string userId);

    Task<bool> MarkAsReadAsync(
        string userId,
        int notificationId);

    Task MarkAllAsReadAsync(
        string userId);

    Task<int> DeleteReadAsync(
        string userId);

    Task CreateForAdminsAsync(
        CreateAdminNotificationDto request);
}
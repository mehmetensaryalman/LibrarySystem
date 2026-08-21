using LibrarySystem.Domain.Entities;

namespace LibrarySystem.Application.Interfaces.Repositories;

public interface INotificationRepository
{
    Task<IReadOnlyList<Notification>>
        GetLatestByRecipientAsync(
            string recipientUserId,
            int take);

    Task<int> GetUnreadCountAsync(
        string recipientUserId);

    Task<Notification?> GetByIdAsync(
        int notificationId,
        string recipientUserId);

    Task<IReadOnlyList<string>>
        GetUserIdsInRoleAsync(
            string roleName);

    Task AddRangeAsync(
        IEnumerable<Notification> notifications);

    Task SaveChangesAsync();

    Task MarkAllAsReadAsync(
        string recipientUserId,
        DateTime readAt);
}
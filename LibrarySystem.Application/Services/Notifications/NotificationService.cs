using LibrarySystem.Application.Common.Constants;
using LibrarySystem.Application.DTOs.Notifications;
using LibrarySystem.Application.Interfaces.Notifications;
using LibrarySystem.Application.Interfaces.Repositories;
using LibrarySystem.Domain.Entities;

namespace LibrarySystem.Application.Services.Notifications;

public class NotificationService :
    INotificationService
{
    private const int LatestNotificationCount =
        10;

    private readonly
        INotificationRepository
        _notificationRepository;

    public NotificationService(
        INotificationRepository
            notificationRepository)
    {
        _notificationRepository =
            notificationRepository;
    }

    public async Task<NotificationSummaryDto>
        GetMySummaryAsync(
            string userId)
    {
        var notifications =
            await _notificationRepository
                .GetLatestByRecipientAsync(
                    userId,
                    LatestNotificationCount);

        var unreadCount =
            await _notificationRepository
                .GetUnreadCountAsync(
                    userId);

        return new NotificationSummaryDto
        {
            UnreadCount =
                unreadCount,

            Notifications =
                notifications
                    .Select(Map)
                    .ToList()
        };
    }

    public async Task<bool>
        MarkAsReadAsync(
            string userId,
            int notificationId)
    {
        var notification =
            await _notificationRepository
                .GetByIdAsync(
                    notificationId,
                    userId);

        if (notification is null)
        {
            return false;
        }

        if (notification.IsRead)
        {
            return true;
        }

        notification.IsRead =
            true;

        notification.ReadAt =
            DateTime.UtcNow;

        await _notificationRepository
            .SaveChangesAsync();

        return true;
    }

    public Task MarkAllAsReadAsync(
        string userId)
    {
        return _notificationRepository
            .MarkAllAsReadAsync(
                userId,
                DateTime.UtcNow);
    }

    public Task<int> DeleteReadAsync(
        string userId)
    {
        return _notificationRepository
            .DeleteReadAsync(
                userId);
    }

    public async Task
        CreateForAdminsAsync(
            CreateAdminNotificationDto
                request)
    {
        var adminUserIds =
            await _notificationRepository
                .GetUserIdsInRoleAsync(
                    RoleNames.Admin);

        if (adminUserIds.Count == 0)
        {
            return;
        }

        var createdAt =
            DateTime.UtcNow;

        var notifications =
            adminUserIds
                .Select(adminUserId =>
                    new Notification
                    {
                        RecipientUserId =
                            adminUserId,

                        Type =
                            request.Type,

                        Title =
                            request.Title,

                        Message =
                            request.Message,

                        BorrowRecordId =
                            request.BorrowRecordId,

                        IsRead =
                            false,

                        CreatedAt =
                            createdAt,

                        ReadAt =
                            null
                    })
                .ToList();

        await _notificationRepository
            .AddRangeAsync(
                notifications);

        await _notificationRepository
            .SaveChangesAsync();
    }

    private static NotificationDto Map(
        Notification notification)
    {
        return new NotificationDto
        {
            Id =
                notification.Id,

            Type =
                notification.Type
                    .ToString(),

            Title =
                notification.Title,

            Message =
                notification.Message,

            BorrowRecordId =
                notification.BorrowRecordId,

            IsRead =
                notification.IsRead,

            CreatedAt =
                AsUtc(
                    notification.CreatedAt),

            ReadAt =
                AsUtc(
                    notification.ReadAt)
        };
    }

    private static DateTime AsUtc(
        DateTime value)
    {
        return value.Kind ==
               DateTimeKind.Utc
            ? value
            : DateTime.SpecifyKind(
                value,
                DateTimeKind.Utc);
    }

    private static DateTime? AsUtc(
        DateTime? value)
    {
        return value.HasValue
            ? AsUtc(
                value.Value)
            : null;
    }
}
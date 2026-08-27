using LibrarySystem.Application.Interfaces.Repositories;
using LibrarySystem.Domain.Entities;
using LibrarySystem.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace LibrarySystem.Infrastructure.Repositories;

public class NotificationRepository :
    INotificationRepository
{
    private readonly LibraryDbContext
        _dbContext;

    public NotificationRepository(
        LibraryDbContext dbContext)
    {
        _dbContext =
            dbContext;
    }

    public async Task<
        IReadOnlyList<Notification>>
        GetLatestByRecipientAsync(
            string recipientUserId,
            int take)
    {
        return await _dbContext
            .Notifications
            .AsNoTracking()
            .Where(x =>
                x.RecipientUserId ==
                recipientUserId)
            .OrderByDescending(x =>
                x.CreatedAt)
            .ThenByDescending(x =>
                x.Id)
            .Take(take)
            .ToListAsync();
    }

    public Task<int>
        GetUnreadCountAsync(
            string recipientUserId)
    {
        return _dbContext
            .Notifications
            .CountAsync(x =>
                x.RecipientUserId ==
                    recipientUserId &&
                !x.IsRead);
    }

    public Task<Notification?>
        GetByIdAsync(
            int notificationId,
            string recipientUserId)
    {
        return _dbContext
            .Notifications
            .FirstOrDefaultAsync(x =>
                x.Id ==
                    notificationId &&
                x.RecipientUserId ==
                    recipientUserId);
    }

    public async Task<
        IReadOnlyList<string>>
        GetUserIdsInRoleAsync(
            string roleName)
    {
        return await (
            from user in
                _dbContext.Users
            join userRole in
                _dbContext.UserRoles
                on user.Id
                equals userRole.UserId
            join role in
                _dbContext.Roles
                on userRole.RoleId
                equals role.Id
            where role.Name ==
                  roleName
            select user.Id
        )
            .Distinct()
            .ToListAsync();
    }

    public async Task AddRangeAsync(
        IEnumerable<Notification>
            notifications)
    {
        await _dbContext
            .Notifications
            .AddRangeAsync(
                notifications);
    }

    public Task SaveChangesAsync()
    {
        return _dbContext
            .SaveChangesAsync();
    }

    public Task MarkAllAsReadAsync(
        string recipientUserId,
        DateTime readAt)
    {
        return _dbContext
            .Notifications
            .Where(x =>
                x.RecipientUserId ==
                    recipientUserId &&
                !x.IsRead)
            .ExecuteUpdateAsync(
                setters =>
                    setters
                        .SetProperty(
                            x =>
                                x.IsRead,
                            true)
                        .SetProperty(
                            x =>
                                x.ReadAt,
                            readAt));
    }

    public Task<int> DeleteReadAsync(
        string recipientUserId)
    {
        return _dbContext
            .Notifications
            .Where(x =>
                x.RecipientUserId ==
                    recipientUserId &&
                x.IsRead)
            .ExecuteDeleteAsync();
    }
}
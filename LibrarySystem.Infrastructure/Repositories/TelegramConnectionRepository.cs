using LibrarySystem.Application.Interfaces.Repositories;
using LibrarySystem.Domain.Entities;
using LibrarySystem.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace LibrarySystem.Infrastructure.Repositories;

public class TelegramConnectionRepository :
    ITelegramConnectionRepository
{
    private readonly LibraryDbContext
        _dbContext;

    public TelegramConnectionRepository(
        LibraryDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<TelegramConnection?>
        GetByUserIdAsync(
            string userId)
    {
        return _dbContext
            .TelegramConnections
            .FirstOrDefaultAsync(
                connection =>
                    connection.UserId ==
                    userId);
    }

    public Task<TelegramConnection?>
        GetByChatIdAsync(
            long chatId)
    {
        return _dbContext
            .TelegramConnections
            .FirstOrDefaultAsync(
                connection =>
                    connection.ChatId ==
                    chatId);
    }

    public Task<TelegramConnection?>
        GetByConnectionCodeHashAsync(
            string connectionCodeHash,
            DateTime currentDate)
    {
        return _dbContext
            .TelegramConnections
            .FirstOrDefaultAsync(
                connection =>
                    connection
                        .ConnectionCodeHash ==
                    connectionCodeHash &&
                    connection
                        .ConnectionCodeExpiresAt
                        .HasValue &&
                    connection
                        .ConnectionCodeExpiresAt
                        .Value >
                    currentDate);
    }

    public async Task AddAsync(
        TelegramConnection connection)
    {
        await _dbContext
            .TelegramConnections
            .AddAsync(connection);
    }

    public Task SaveChangesAsync()
    {
        return _dbContext
            .SaveChangesAsync();
    }
}

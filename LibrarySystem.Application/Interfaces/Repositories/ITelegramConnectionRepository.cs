using LibrarySystem.Domain.Entities;

namespace LibrarySystem.Application.Interfaces.Repositories;

public interface ITelegramConnectionRepository
{
    Task<TelegramConnection?>
        GetByUserIdAsync(
            string userId);

    Task<TelegramConnection?>
        GetByChatIdAsync(
            long chatId);

    Task<TelegramConnection?>
        GetByConnectionCodeHashAsync(
            string connectionCodeHash,
            DateTime currentDate);

    Task AddAsync(
        TelegramConnection connection);

    Task SaveChangesAsync();
}

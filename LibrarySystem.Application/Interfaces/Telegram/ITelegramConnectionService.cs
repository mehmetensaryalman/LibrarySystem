using LibrarySystem.Application.DTOs.Telegram;

namespace LibrarySystem.Application.Interfaces.Telegram;

public interface ITelegramConnectionService
{
    Task<TelegramConnectionStatusDto>
        GetStatusAsync(
            string userId);

    Task<TelegramConnectionLinkDto>
        CreateConnectionLinkAsync(
            string userId);

    Task<TelegramConnectionCompletionDto>
        CompleteConnectionAsync(
            string connectionCode,
            long chatId,
            string? telegramUsername);

    Task DisconnectAsync(
        string userId);
}

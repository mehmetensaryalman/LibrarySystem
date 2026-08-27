using LibrarySystem.Application.DTOs.Telegram;

namespace LibrarySystem.Application.Interfaces.Telegram;

public interface ITelegramBotService
{
    bool IsConfigured { get; }

    string BotUsername { get; }

    Task<IReadOnlyList<
        TelegramIncomingUpdateDto>>
        GetUpdatesAsync(
            long offset,
            CancellationToken
                cancellationToken = default);

    Task SendMessageAsync(
        long chatId,
        string message,
        CancellationToken
            cancellationToken = default);
}

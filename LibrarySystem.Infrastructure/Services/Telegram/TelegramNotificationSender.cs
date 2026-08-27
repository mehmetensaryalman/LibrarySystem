using LibrarySystem.Application.Interfaces.Repositories;
using LibrarySystem.Application.Interfaces.Telegram;
using Microsoft.Extensions.Logging;

namespace LibrarySystem.Infrastructure.Services.Telegram;

public sealed class TelegramNotificationSender :
    ITelegramNotificationSender
{
    private readonly
        ITelegramConnectionRepository
            _telegramConnectionRepository;

    private readonly
        ITelegramBotService
            _telegramBotService;

    private readonly
        ILogger<TelegramNotificationSender>
            _logger;

    public TelegramNotificationSender(
        ITelegramConnectionRepository
            telegramConnectionRepository,
        ITelegramBotService
            telegramBotService,
        ILogger<TelegramNotificationSender>
            logger)
    {
        _telegramConnectionRepository =
            telegramConnectionRepository;

        _telegramBotService =
            telegramBotService;

        _logger =
            logger;
    }

    public async Task SendToUserAsync(
        string userId,
        string message,
        CancellationToken cancellationToken =
            default)
    {
        if (
            string.IsNullOrWhiteSpace(userId) ||
            string.IsNullOrWhiteSpace(message))
        {
            return;
        }

        try
        {
            var connection =
                await _telegramConnectionRepository
                    .GetByUserIdAsync(userId);

            if (
                connection is null ||
                !connection.IsEnabled ||
                !connection.ChatId.HasValue)
            {
                return;
            }

            await _telegramBotService
                .SendMessageAsync(
                    connection.ChatId.Value,
                    message.Trim(),
                    cancellationToken);
        }
        catch (OperationCanceledException)
            when (
                cancellationToken
                    .IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            _logger.LogWarning(
                exception,
                "Telegram notification could not be sent to user {UserId}.",
                userId);
        }
    }
}
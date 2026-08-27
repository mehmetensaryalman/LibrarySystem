using LibrarySystem.Application.DTOs.Telegram;
using LibrarySystem.Application.Interfaces.Telegram;

namespace LibrarySystem.Api.BackgroundServices;

public sealed class TelegramBotPollingService :
    BackgroundService
{
    private readonly
        ITelegramBotService
            _telegramBotService;

    private readonly ILogger<
        TelegramBotPollingService>
        _logger;

    private readonly
        IServiceScopeFactory
            _serviceScopeFactory;

    public TelegramBotPollingService(
        ITelegramBotService
            telegramBotService,
        ILogger<
            TelegramBotPollingService>
            logger,
        IServiceScopeFactory
            serviceScopeFactory)
    {
        _telegramBotService =
            telegramBotService;

        _logger = logger;

        _serviceScopeFactory =
            serviceScopeFactory;
    }

    protected override async Task
        ExecuteAsync(
            CancellationToken
                stoppingToken)
    {
        if (
            !_telegramBotService
                .IsConfigured)
        {
            _logger.LogWarning(
                "Telegram bot token is missing. Telegram listener was not started.");

            return;
        }

        _logger.LogInformation(
            "Telegram bot listener started.");

        long offset = 0;

        while (!stoppingToken
                   .IsCancellationRequested)
        {
            try
            {
                var updates =
                    await _telegramBotService
                        .GetUpdatesAsync(
                            offset,
                            stoppingToken);

                foreach (
                    var update
                    in updates.OrderBy(
                        item =>
                            item.UpdateId))
                {
                    offset = Math.Max(
                        offset,
                        update.UpdateId + 1);

                    await HandleUpdateAsync(
                        update,
                        stoppingToken);
                }
            }
            catch (
                OperationCanceledException)
                when (
                    stoppingToken
                        .IsCancellationRequested)
            {
                break;
            }
            catch (Exception exception)
            {
                _logger.LogError(
                    exception,
                    "An error occurred while processing Telegram updates.");

                await Task.Delay(
                    TimeSpan.FromSeconds(5),
                    stoppingToken);
            }
        }
    }

    private async Task HandleUpdateAsync(
        TelegramIncomingUpdateDto update,
        CancellationToken
            cancellationToken)
    {
        if (
            !update.ChatId.HasValue ||
            string.IsNullOrWhiteSpace(
                update.Text))
        {
            return;
        }

        var commandParts =
            update.Text
                .Trim()
                .Split(
                    ' ',
                    2,
                    StringSplitOptions
                        .RemoveEmptyEntries);

        var command =
            commandParts[0]
                .Split('@')[0];

        if (
            command.Equals(
                "/start",
                StringComparison
                    .OrdinalIgnoreCase))
        {
            if (commandParts.Length == 2)
            {
                using var scope =
                    _serviceScopeFactory
                        .CreateScope();

                var connectionService =
                    scope.ServiceProvider
                        .GetRequiredService<
                            ITelegramConnectionService>();

                var connectionResult =
                    await connectionService
                        .CompleteConnectionAsync(
                            commandParts[1],
                            update.ChatId.Value,
                            update.Username);

                await _telegramBotService
                    .SendMessageAsync(
                        update.ChatId.Value,
                        connectionResult.Message,
                        cancellationToken);

                return;
            }

            var greeting =
                string.IsNullOrWhiteSpace(
                    update.FirstName)
                    ? "Merhaba!"
                    : $"Merhaba {update.FirstName}!";

            await _telegramBotService
                .SendMessageAsync(
                    update.ChatId.Value,
                    $"{greeting}\n\nKütüphane Sistemi Bildirim Botu başarıyla çalışıyor. Hesap bağlantısı tamamlandıktan sonra ödünç ve iade hatırlatmalarınızı buradan alabileceksiniz.",
                    cancellationToken);

            return;
        }

        if (
            command.Equals(
                "/help",
                StringComparison
                    .OrdinalIgnoreCase))
        {
            await _telegramBotService
                .SendMessageAsync(
                    update.ChatId.Value,
                    "Kullanılabilir komutlar:\n/start - Bot bağlantısını kontrol eder\n/help - Yardım mesajını gösterir",
                    cancellationToken);
        }
    }
}

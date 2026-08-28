using LibrarySystem.Application.Interfaces.Telegram;
using LibrarySystem.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace LibrarySystem.Api.BackgroundServices;

public sealed class TelegramBorrowReminderService :
    BackgroundService
{
    private static readonly TimeSpan
        CheckInterval =
            TimeSpan.FromMinutes(15);

    private readonly IServiceScopeFactory
        _serviceScopeFactory;

    private readonly ITelegramBotService
        _telegramBotService;

    private readonly ILogger<
        TelegramBorrowReminderService>
        _logger;

    public TelegramBorrowReminderService(
        IServiceScopeFactory
            serviceScopeFactory,
        ITelegramBotService
            telegramBotService,
        ILogger<
            TelegramBorrowReminderService>
            logger)
    {
        _serviceScopeFactory =
            serviceScopeFactory;

        _telegramBotService =
            telegramBotService;

        _logger =
            logger;
    }

    protected override async Task
        ExecuteAsync(
            CancellationToken
                stoppingToken)
    {
        if (!_telegramBotService.IsConfigured)
        {
            _logger.LogWarning(
                "Telegram bot token is missing. Borrow reminder service was not started.");

            return;
        }

        _logger.LogInformation(
            "Telegram borrow reminder service started.");

        await CheckAndSendRemindersAsync(
            stoppingToken);

        using var timer =
            new PeriodicTimer(
                CheckInterval);

        while (
            await timer.WaitForNextTickAsync(
                stoppingToken))
        {
            await CheckAndSendRemindersAsync(
                stoppingToken);
        }
    }

    private async Task
        CheckAndSendRemindersAsync(
            CancellationToken
                cancellationToken)
    {
        try
        {
            using var scope =
                _serviceScopeFactory
                    .CreateScope();

            var dbContext =
                scope.ServiceProvider
                    .GetRequiredService<
                        LibraryDbContext>();

            var currentDate =
                DateTime.UtcNow;

            var candidates =
                await (
                    from borrowRecord
                        in dbContext
                            .BorrowRecords
                            .Include(record =>
                                record.Book)
                    join telegramConnection
                        in dbContext
                            .TelegramConnections
                        on borrowRecord.UserId
                        equals telegramConnection.UserId
                    where
                        !borrowRecord.IsReturned &&
                        telegramConnection.IsEnabled &&
                        telegramConnection.ChatId.HasValue &&
                        (
                            !borrowRecord.ThreeDaysReminderSentAt.HasValue ||
                            !borrowRecord.DueDateReminderSentAt.HasValue ||
                            !borrowRecord.OverdueReminderSentAt.HasValue
                        )
                    orderby borrowRecord.DueDate
                    select new
                    {
                        BorrowRecord =
                            borrowRecord,

                        ChatId =
                            telegramConnection
                                .ChatId!
                                .Value
                    })
                    .ToListAsync(
                        cancellationToken);

            foreach (var candidate in candidates)
            {
                cancellationToken
                    .ThrowIfCancellationRequested();

                var record =
                    candidate.BorrowRecord;

                var remainingTime =
                    record.DueDate -
                    currentDate;

                string? message = null;

                Action markAsSent =
                    static () =>
                    {
                    };

                if (
                    remainingTime <=
                        TimeSpan.Zero &&
                    !record
                        .OverdueReminderSentAt
                        .HasValue)
                {
                    message =
                        "⚠️ Kitabınızın İade Süresi Geçti\n\n" +
                        $"\"{record.Book.Name}\" kitabının son iade tarihi geçmiştir.\n\n" +
                        "Yeni ödünç talebi oluşturabilmek ve ceza süresinin daha fazla uzamasını önlemek için kitabı en kısa sürede kütüphaneye teslim ediniz.";

                    markAsSent = () =>
                        record.OverdueReminderSentAt =
                            currentDate;
                }
                else if (
                    remainingTime >
                        TimeSpan.Zero &&
                    remainingTime <=
                        TimeSpan.FromDays(1) &&
                    !record
                        .DueDateReminderSentAt
                        .HasValue)
                {
                    message =
                        "⏰ Kitabınızın Son İade Günü\n\n" +
                        $"\"{record.Book.Name}\" kitabının iade süresi bugün doluyor.\n\n" +
                        $"Son iade zamanı: {FormatTurkeyTime(record.DueDate)}\n\n" +
                        "Gecikme cezası oluşmaması için kitabı zamanında kütüphaneye teslim ediniz.";

                    markAsSent = () =>
                        record.DueDateReminderSentAt =
                            currentDate;
                }
                else if (
                    remainingTime >
                        TimeSpan.FromDays(1) &&
                    remainingTime <=
                        TimeSpan.FromDays(3) &&
                    !record
                        .ThreeDaysReminderSentAt
                        .HasValue)
                {
                    message =
                        "📅 İade Tarihi Yaklaşıyor\n\n" +
                        $"\"{record.Book.Name}\" kitabının iade süresinin dolmasına 3 gün kaldı.\n\n" +
                        $"Son iade zamanı: {FormatTurkeyTime(record.DueDate)}";

                    markAsSent = () =>
                        record.ThreeDaysReminderSentAt =
                            currentDate;
                }

                if (message is null)
                {
                    continue;
                }

                try
                {
                    await _telegramBotService
                        .SendMessageAsync(
                            candidate.ChatId,
                            message,
                            cancellationToken);

                    markAsSent();

                    await dbContext
                        .SaveChangesAsync(
                            cancellationToken);
                }
                catch (
                    OperationCanceledException)
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
                        "Telegram reminder could not be sent for borrow record {BorrowRecordId}.",
                        record.Id);
                }
            }
        }
        catch (
            OperationCanceledException)
            when (
                cancellationToken
                    .IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            _logger.LogError(
                exception,
                "An error occurred while checking Telegram borrow reminders.");
        }
    }

    private static string
        FormatTurkeyTime(
            DateTime utcDateTime)
    {
        var normalizedUtcDateTime =
            DateTime.SpecifyKind(
                utcDateTime,
                DateTimeKind.Utc);

        TimeZoneInfo turkeyTimeZone;

        try
        {
            turkeyTimeZone =
                TimeZoneInfo
                    .FindSystemTimeZoneById(
                        "Europe/Istanbul");
        }
        catch (
            TimeZoneNotFoundException)
        {
            turkeyTimeZone =
                TimeZoneInfo
                    .FindSystemTimeZoneById(
                        "Turkey Standard Time");
        }

        return TimeZoneInfo
            .ConvertTimeFromUtc(
                normalizedUtcDateTime,
                turkeyTimeZone)
            .ToString(
                "dd.MM.yyyy HH:mm");
    }
}

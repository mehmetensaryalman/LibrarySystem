using System.Security.Cryptography;
using System.Text;
using LibrarySystem.Application.DTOs.Telegram;
using LibrarySystem.Application.Interfaces.Repositories;
using LibrarySystem.Application.Interfaces.Telegram;
using LibrarySystem.Domain.Entities;

namespace LibrarySystem.Application.Services.Telegram;

public class TelegramConnectionService :
    ITelegramConnectionService
{
    private const int
        ConnectionCodeLifetimeMinutes = 10;

    private readonly
        ITelegramConnectionRepository
            _telegramConnectionRepository;

    private readonly
        ITelegramBotService
            _telegramBotService;

    public TelegramConnectionService(
        ITelegramConnectionRepository
            telegramConnectionRepository,
        ITelegramBotService
            telegramBotService)
    {
        _telegramConnectionRepository =
            telegramConnectionRepository;

        _telegramBotService =
            telegramBotService;
    }

    public async Task<
        TelegramConnectionStatusDto>
        GetStatusAsync(
            string userId)
    {
        var connection =
            await _telegramConnectionRepository
                .GetByUserIdAsync(
                    userId);

        if (
            connection is null ||
            !connection.ChatId.HasValue)
        {
            return new
                TelegramConnectionStatusDto
            {
                IsConnected = false,
                IsEnabled = false,
                TelegramUsername = null,
                ConnectedAt = null
            };
        }

        return new TelegramConnectionStatusDto
        {
            IsConnected = true,
            IsEnabled =
                connection.IsEnabled,

            TelegramUsername =
                connection
                    .TelegramUsername,

            ConnectedAt =
                AsUtc(
                    connection.ConnectedAt)
        };
    }

    public async Task<
        TelegramConnectionLinkDto>
        CreateConnectionLinkAsync(
            string userId)
    {
        var botUsername =
            _telegramBotService
                .BotUsername
                .Trim()
                .TrimStart('@');

        if (
            string.IsNullOrWhiteSpace(
                botUsername))
        {
            throw new InvalidOperationException(
                "Telegram bot username configuration is missing.");
        }

        var currentDate =
            DateTime.UtcNow;

        var expiresAt =
            currentDate.AddMinutes(
                ConnectionCodeLifetimeMinutes);

        var connectionCode =
            Convert
                .ToHexString(
                    RandomNumberGenerator
                        .GetBytes(16))
                .ToLowerInvariant();

        var connectionCodeHash =
            HashConnectionCode(
                connectionCode);

        var connection =
            await _telegramConnectionRepository
                .GetByUserIdAsync(
                    userId);

        if (connection is null)
        {
            connection =
                new TelegramConnection
                {
                    UserId = userId,
                    ChatId = null,
                    TelegramUsername = null,
                    ConnectionCodeHash =
                        connectionCodeHash,
                    ConnectionCodeExpiresAt =
                        expiresAt,
                    ConnectedAt = null,
                    IsEnabled = false,
                    CreatedAt = currentDate,
                    UpdatedAt = currentDate
                };

            await _telegramConnectionRepository
                .AddAsync(connection);
        }
        else
        {
            connection.ConnectionCodeHash =
                connectionCodeHash;

            connection
                    .ConnectionCodeExpiresAt =
                expiresAt;

            connection.UpdatedAt =
                currentDate;
        }

        await _telegramConnectionRepository
            .SaveChangesAsync();

        return new TelegramConnectionLinkDto
        {
            ConnectionUrl =
                $"https://t.me/{botUsername}?start={connectionCode}",

            ExpiresAt =
                AsUtc(expiresAt)
        };
    }

    public async Task<
        TelegramConnectionCompletionDto>
        CompleteConnectionAsync(
            string connectionCode,
            long chatId,
            string? telegramUsername)
    {
        var normalizedCode =
            connectionCode.Trim();

        if (
            string.IsNullOrWhiteSpace(
                normalizedCode) ||
            normalizedCode.Length > 100)
        {
            return Failure(
                "Telegram bağlantı kodu geçersiz.");
        }

        var currentDate =
            DateTime.UtcNow;

        var connectionCodeHash =
            HashConnectionCode(
                normalizedCode);

        var connection =
            await _telegramConnectionRepository
                .GetByConnectionCodeHashAsync(
                    connectionCodeHash,
                    currentDate);

        if (connection is null)
        {
            return Failure(
                "Telegram bağlantı kodu geçersiz veya süresi dolmuş. Uygulama üzerinden yeni bağlantı oluşturunuz.");
        }

        var existingChatConnection =
            await _telegramConnectionRepository
                .GetByChatIdAsync(
                    chatId);

        if (
            existingChatConnection is not null &&
            existingChatConnection.UserId !=
            connection.UserId)
        {
            return Failure(
                "Bu Telegram hesabı başka bir LibrarySystem kullanıcısına bağlıdır.");
        }

        connection.ChatId = chatId;

        connection.TelegramUsername =
            NormalizeUsername(
                telegramUsername);

        connection.ConnectedAt =
            currentDate;

        connection.IsEnabled = true;

        connection.ConnectionCodeHash =
            null;

        connection.ConnectionCodeExpiresAt =
            null;

        connection.UpdatedAt =
            currentDate;

        await _telegramConnectionRepository
            .SaveChangesAsync();

        return new
            TelegramConnectionCompletionDto
        {
            Success = true,
            Message =
                "Telegram hesabınız Kütüphane Sistemi hesabınıza başarıyla bağlandı. Ödünç ve iade hatırlatmalarınızı artık buradan alabilirsiniz."
        };
    }

    public async Task DisconnectAsync(
        string userId)
    {
        var connection =
            await _telegramConnectionRepository
                .GetByUserIdAsync(
                    userId);

        if (connection is null)
        {
            return;
        }

        connection.ChatId = null;
        connection.TelegramUsername = null;
        connection.ConnectedAt = null;
        connection.IsEnabled = false;
        connection.ConnectionCodeHash = null;

        connection.ConnectionCodeExpiresAt =
            null;

        connection.UpdatedAt =
            DateTime.UtcNow;

        await _telegramConnectionRepository
            .SaveChangesAsync();
    }

    private static string
        HashConnectionCode(
            string connectionCode)
    {
        var bytes =
            Encoding.UTF8.GetBytes(
                connectionCode);

        return Convert.ToHexString(
            SHA256.HashData(bytes));
    }

    private static string?
        NormalizeUsername(
            string? telegramUsername)
    {
        if (
            string.IsNullOrWhiteSpace(
                telegramUsername))
        {
            return null;
        }

        return telegramUsername
            .Trim()
            .TrimStart('@');
    }

    private static
        TelegramConnectionCompletionDto
        Failure(
            string message)
    {
        return new
            TelegramConnectionCompletionDto
        {
            Success = false,
            Message = message
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
            ? AsUtc(value.Value)
            : null;
    }
}

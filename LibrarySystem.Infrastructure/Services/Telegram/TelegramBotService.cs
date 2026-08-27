using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;
using LibrarySystem.Application.DTOs.Telegram;
using LibrarySystem.Application.Interfaces.Telegram;
using Microsoft.Extensions.Options;

namespace LibrarySystem.Infrastructure.Services.Telegram;

public sealed class TelegramBotService :
    ITelegramBotService,
    IDisposable
{
    private readonly HttpClient _httpClient;

    private readonly JsonSerializerOptions
        _serializerOptions;

    public bool IsConfigured { get; }

    public string BotUsername { get; }

    public TelegramBotService(
        IOptions<TelegramBotOptions>
            options)
    {
        var botToken =
            options.Value.BotToken.Trim();

        BotUsername =
            options.Value
                .BotUsername
                .Trim()
                .TrimStart('@');

        IsConfigured =
            !string.IsNullOrWhiteSpace(
                botToken);

        if (
            IsConfigured &&
            ContainsInvalidTokenCharacter(
                botToken))
        {
            throw new InvalidOperationException(
                "Telegram bot token configuration is invalid.");
        }

        var baseAddress =
            IsConfigured
                ? $"https://api.telegram.org/bot{botToken}/"
                : "https://api.telegram.org/";

        _httpClient =
            new HttpClient
            {
                BaseAddress =
                    new Uri(baseAddress),

                Timeout =
                    TimeSpan.FromSeconds(40)
            };

        _serializerOptions =
            new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive =
                    true
            };
    }

    public async Task<IReadOnlyList<
        TelegramIncomingUpdateDto>>
        GetUpdatesAsync(
            long offset,
            CancellationToken
                cancellationToken = default)
    {
        EnsureConfigured();

        var requestUri =
            $"getUpdates?offset={offset}&timeout=25";

        using var response =
            await _httpClient.GetAsync(
                requestUri,
                cancellationToken);

        var responseContent =
            await response.Content
                .ReadAsStringAsync(
                    cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException(
                $"Telegram güncellemeleri alınamadı. HTTP durum kodu: {(int)response.StatusCode}.");
        }

        var result =
            JsonSerializer.Deserialize<
                TelegramApiResponse<
                    List<TelegramUpdate>>>(
                responseContent,
                _serializerOptions);

        if (result?.Ok != true)
        {
            throw new InvalidOperationException(
                "Telegram güncellemeleri alınamadı.");
        }

        return result.Result
            .Select(update =>
                new TelegramIncomingUpdateDto
                {
                    UpdateId =
                        update.UpdateId,

                    ChatId =
                        update.Message?.Chat.Id,

                    Username =
                        update.Message?.From
                            ?.Username,

                    FirstName =
                        update.Message?.From
                            ?.FirstName,

                    Text =
                        update.Message?.Text
                })
            .ToList();
    }

    public async Task SendMessageAsync(
        long chatId,
        string message,
        CancellationToken
            cancellationToken = default)
    {
        EnsureConfigured();

        using var content =
            new FormUrlEncodedContent(
                new Dictionary<string, string>
                {
                    ["chat_id"] =
                        chatId.ToString(
                            CultureInfo
                                .InvariantCulture),

                    ["text"] =
                        message
                });

        using var response =
            await _httpClient.PostAsync(
                "sendMessage",
                content,
                cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException(
                $"Telegram mesajı gönderilemedi. HTTP durum kodu: {(int)response.StatusCode}.");
        }
    }

    public void Dispose()
    {
        _httpClient.Dispose();
    }

    private void EnsureConfigured()
    {
        if (!IsConfigured)
        {
            throw new InvalidOperationException(
                "Telegram bot configuration is missing.");
        }
    }

    private static bool
        ContainsInvalidTokenCharacter(
            string token)
    {
        return token.Contains('/') ||
               token.Contains('?') ||
               token.Contains('#') ||
               token.Any(char.IsWhiteSpace);
    }

    private sealed class TelegramApiResponse<
        TResult>
    {
        [JsonPropertyName("ok")]
        public bool Ok { get; set; }

        [JsonPropertyName("result")]
        public TResult Result { get; set; }
            = default!;
    }

    private sealed class TelegramUpdate
    {
        [JsonPropertyName("update_id")]
        public long UpdateId { get; set; }

        [JsonPropertyName("message")]
        public TelegramMessage? Message
        {
            get;
            set;
        }
    }

    private sealed class TelegramMessage
    {
        [JsonPropertyName("from")]
        public TelegramUser? From
        {
            get;
            set;
        }

        [JsonPropertyName("chat")]
        public TelegramChat Chat { get; set; }
            = null!;

        [JsonPropertyName("text")]
        public string? Text { get; set; }
    }

    private sealed class TelegramUser
    {
        [JsonPropertyName("first_name")]
        public string? FirstName { get; set; }

        [JsonPropertyName("username")]
        public string? Username { get; set; }
    }

    private sealed class TelegramChat
    {
        [JsonPropertyName("id")]
        public long Id { get; set; }
    }
}

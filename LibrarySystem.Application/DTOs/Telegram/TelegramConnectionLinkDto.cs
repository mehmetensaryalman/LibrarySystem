namespace LibrarySystem.Application.DTOs.Telegram;

public class TelegramConnectionLinkDto
{
    public string ConnectionUrl { get; set; }
        = string.Empty;

    public DateTime ExpiresAt { get; set; }
}

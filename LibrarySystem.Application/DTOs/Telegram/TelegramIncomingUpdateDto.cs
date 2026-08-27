namespace LibrarySystem.Application.DTOs.Telegram;

public class TelegramIncomingUpdateDto
{
    public long UpdateId { get; set; }

    public long? ChatId { get; set; }

    public string? Username { get; set; }

    public string? FirstName { get; set; }

    public string? Text { get; set; }
}

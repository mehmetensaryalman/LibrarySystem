namespace LibrarySystem.Application.DTOs.Telegram;

public class TelegramConnectionCompletionDto
{
    public bool Success { get; set; }

    public string Message { get; set; }
        = string.Empty;
}

namespace LibrarySystem.Application.DTOs.Telegram;

public class TelegramConnectionStatusDto
{
    public bool IsConnected { get; set; }

    public bool IsEnabled { get; set; }

    public string? TelegramUsername
    {
        get;
        set;
    }

    public DateTime? ConnectedAt
    {
        get;
        set;
    }
}

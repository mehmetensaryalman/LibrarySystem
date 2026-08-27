namespace LibrarySystem.Domain.Entities;

public class TelegramConnection
{
    public int Id { get; set; }

    public string UserId { get; set; }
        = string.Empty;

    public long? ChatId { get; set; }

    public string? TelegramUsername
    {
        get;
        set;
    }

    public string? ConnectionCodeHash
    {
        get;
        set;
    }

    public DateTime?
        ConnectionCodeExpiresAt
    {
        get;
        set;
    }

    public DateTime? ConnectedAt
    {
        get;
        set;
    }

    public bool IsEnabled { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }
}

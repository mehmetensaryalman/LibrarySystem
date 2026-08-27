namespace LibrarySystem.Application.Interfaces.Telegram;

public interface ITelegramNotificationSender
{
    Task SendToUserAsync(
        string userId,
        string message,
        CancellationToken cancellationToken =
            default);
}
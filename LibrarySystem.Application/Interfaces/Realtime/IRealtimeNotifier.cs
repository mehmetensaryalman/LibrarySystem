namespace LibrarySystem.Application.Interfaces.Realtime;

public interface IRealtimeNotifier
{
    Task NotifyBooksChangedAsync();

    Task NotifyBorrowsChangedAsync();
}
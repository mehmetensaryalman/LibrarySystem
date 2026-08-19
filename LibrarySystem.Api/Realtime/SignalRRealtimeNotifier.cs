using LibrarySystem.Api.Hubs;
using LibrarySystem.Application.DTOs.Borrow;
using LibrarySystem.Application.Interfaces.Realtime;
using Microsoft.AspNetCore.SignalR;

namespace LibrarySystem.Api.Realtime;

public class SignalRRealtimeNotifier
    : IRealtimeNotifier
{
    private readonly
        IHubContext<
            LibraryHub,
            ILibraryClient> _hubContext;

    public SignalRRealtimeNotifier(
        IHubContext<
            LibraryHub,
            ILibraryClient> hubContext)
    {
        _hubContext =
            hubContext;
    }

    public async Task
        NotifyBooksChangedAsync()
    {
        await _hubContext
            .Clients
            .All
            .BooksChanged();
    }

    public async Task
        NotifyBorrowsChangedAsync()
    {
        await _hubContext
            .Clients
            .All
            .BorrowsChanged();
    }

    public async Task
        NotifyAdminBorrowNotificationAsync(
            AdminBorrowNotificationDto notification)
    {
        await _hubContext
            .Clients
            .Group(
                LibraryHub.AdminGroupName)
            .AdminBorrowNotification(
                notification);
    }
}
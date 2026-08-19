using LibrarySystem.Application.Common.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace LibrarySystem.Api.Hubs;

[Authorize]
public class LibraryHub
    : Hub<ILibraryClient>
{
    public const string
        AdminGroupName = "Admins";

    public override async Task
        OnConnectedAsync()
    {
        if (
            Context.User?.IsInRole(
                RoleNames.Admin) == true)
        {
            await Groups
                .AddToGroupAsync(
                    Context.ConnectionId,
                    AdminGroupName);
        }

        await base
            .OnConnectedAsync();
    }
}
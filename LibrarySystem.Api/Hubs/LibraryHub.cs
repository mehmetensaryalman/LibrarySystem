using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace LibrarySystem.Api.Hubs;

[Authorize]
public class LibraryHub
    : Hub<ILibraryClient>
{
}
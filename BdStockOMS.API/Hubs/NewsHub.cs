using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace BdStockOMS.API.Hubs;

[Authorize]
public class NewsHub : Hub
{
    public async Task SubscribeToKeyword(string keyword)
        => await Groups.AddToGroupAsync(Context.ConnectionId, $"keyword:{keyword.ToLower().Trim()}");

    public async Task UnsubscribeFromKeyword(string keyword)
        => await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"keyword:{keyword.ToLower().Trim()}");

    public async Task SubscribeToBoard(string board)
        => await Groups.AddToGroupAsync(Context.ConnectionId, $"board:{board.ToUpper()}");

    public async Task UnsubscribeFromBoard(string board)
        => await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"board:{board.ToUpper()}");

    public async Task SubscribeToCategory(string category)
        => await Groups.AddToGroupAsync(Context.ConnectionId, $"category:{category.ToLower()}");

    public async Task UnsubscribeFromCategory(string category)
        => await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"category:{category.ToLower()}");

    public override async Task OnDisconnectedAsync(Exception? exception)
        => await base.OnDisconnectedAsync(exception);
}

// Hubs/StockPriceHub.cs
using Microsoft.AspNetCore.SignalR;

namespace BdStockOMS.API.Hubs;

// Hub = SignalR's communication endpoint
// Think of it like a chat room —
// clients JOIN the room and LISTEN for messages
// Server BROADCASTS to all connected clients
public class StockPriceHub : Hub
{
    // Called automatically when a client connects
    public override async Task OnConnectedAsync()
    {
        await Clients.Caller.SendAsync("Connected",
            $"Connected to BD Stock OMS live feed. ConnectionId: {Context.ConnectionId}");
        await base.OnConnectedAsync();
    }

    // Called automatically when a client disconnects
    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        await base.OnDisconnectedAsync(exception);
    }

    // Client can call this to subscribe to a specific stock
    public async Task SubscribeToStock(string tradingCode)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, tradingCode);
        await Clients.Caller.SendAsync("Subscribed",
            $"Subscribed to {tradingCode} price updates.");
    }

    // Client can unsubscribe from a stock
    public async Task UnsubscribeFromStock(string tradingCode)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, tradingCode);
        await Clients.Caller.SendAsync("Unsubscribed",
            $"Unsubscribed from {tradingCode}.");
    }
}

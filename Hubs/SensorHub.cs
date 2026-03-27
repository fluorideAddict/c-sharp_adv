using Microsoft.AspNetCore.SignalR;
using SenseHatDashboard.Services;

namespace SenseHatDashboard.Hubs;

/// <summary>
/// PATTERN: Observer (hub side)
/// Clients connect here and become observers of the sensor stream.
/// On connect, they immediately receive the latest snapshot so the
/// dashboard doesn't start blank.
/// </summary>
public class SensorHub : Hub
{
    private readonly SensorBroadcastService _broadcastService;

    public SensorHub(SensorBroadcastService broadcastService)
        => _broadcastService = broadcastService;

    public override async Task OnConnectedAsync()
    {
        // Send the current snapshot to the newly connected client
        var snapshot = _broadcastService.LatestSnapshot;
        await Clients.Caller.SendAsync("ReceiveSnapshot", snapshot);
        await base.OnConnectedAsync();
    }
}

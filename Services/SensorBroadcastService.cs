using System.Threading.Channels;
using Microsoft.AspNetCore.SignalR;
using SenseHatDashboard.Hubs;
using SenseHatDashboard.Models;
using SenseHatDashboard.Sensors;
using SenseHatDashboard.Sensors.Decorators;
using SenseHatDashboard.Sensors.Strategies;

namespace SenseHatDashboard.Services;

/// <summary>
/// PATTERN: Publisher/Subscriber
/// Reads all sensors on a timer and publishes SensorReadings into a Channel.
/// The Channel decouples the producer (polling loop) from the consumer (SignalR broadcaster).
///
/// PATTERN: Background Service (ASP.NET Core)
/// Lifecycle is managed by the DI container — starts with the app, stops cleanly on shutdown.
/// </summary>
public class SensorBroadcastService : BackgroundService
{
    private readonly IHubContext<SensorHub> _hubContext;
    private readonly SensorFactory _factory;
    private readonly ILogger<SensorBroadcastService> _logger;

    // The channel is the pub/sub bus between producer and consumer
    private readonly Channel<SensorReading> _channel =
        Channel.CreateBounded<SensorReading>(new BoundedChannelOptions(64)
        {
            FullMode = BoundedChannelFullMode.DropOldest
        });

    // Latest snapshot for new clients connecting mid-session
    private DashboardSnapshot _latest = new(null, null, null, null, null, null);

    public DashboardSnapshot LatestSnapshot => _latest;

    public SensorBroadcastService(
        IHubContext<SensorHub> hubContext,
        SensorFactory factory,
        ILogger<SensorBroadcastService> logger)
    {
        _hubContext = hubContext;
        _factory = factory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var sensors = _factory.CreateAll().ToList();

        // Wire up alert events from any AlertDecorator in the chain
        // PATTERN: Observer — decorators raise events, service observes and forwards
        foreach (var sensor in sensors)
        {
            if (sensor is AlertDecorator alert)
                alert.OnAlert += async evt => await BroadcastAlertAsync(evt);
        }

        // Start producer and consumer concurrently
        await Task.WhenAll(
            ProduceAsync(sensors, stoppingToken),
            ConsumeAsync(stoppingToken)
        );
    }

    /// <summary>Producer: polls sensors every second, writes to channel.</summary>
    private async Task ProduceAsync(IEnumerable<ISensorStrategy> sensors, CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            foreach (var sensor in sensors)
            {
                try
                {
                    var reading = await sensor.ReadAsync(ct);
                    if (reading is not null)
                        await _channel.Writer.WriteAsync(reading, ct);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error reading sensor {Sensor}", sensor.SensorName);
                }
            }

            await Task.Delay(TimeSpan.FromSeconds(1), ct);
        }

        _channel.Writer.Complete();
    }

    /// <summary>Consumer: reads from channel and broadcasts via SignalR.</summary>
    private async Task ConsumeAsync(CancellationToken ct)
    {
        await foreach (var reading in _channel.Reader.ReadAllAsync(ct))
        {
            UpdateSnapshot(reading);

            // PATTERN: Observer — SignalR clients are observers; hub notifies all of them
            await _hubContext.Clients.All.SendAsync("ReceiveReading", reading, ct);
        }
    }

    private async Task BroadcastAlertAsync(AlertEvent alert)
    {
        _logger.LogWarning("[ALERT] {Sensor}: {Message}", alert.SensorName, alert.Message);
        await _hubContext.Clients.All.SendAsync("ReceiveAlert", alert);
    }

    private void UpdateSnapshot(SensorReading r) =>
        _latest = r.SensorName switch
        {
            "Temperature" => _latest with { Temperature = r },
            "Humidity"    => _latest with { Humidity    = r },
            "Pressure"    => _latest with { Pressure    = r },
            "Pitch"       => _latest with { Pitch       = r },
            "Roll"        => _latest with { Roll        = r },
            "Yaw"         => _latest with { Yaw         = r },
            _             => _latest
        };
}

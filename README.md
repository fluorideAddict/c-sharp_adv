# SenseHat Dashboard

ASP.NET Core 8 dashboard served from a Raspberry Pi, streaming live SenseHat
sensor data to any browser on the same network via SignalR.

---

## Design Patterns Used

| Pattern | Where | Why |
|---|---|---|
| **Strategy** | `Sensors/Strategies/` | Each sensor has its own `ISensorStrategy` — swappable, testable, independent |
| **Decorator** | `Sensors/Decorators/` | `LoggingDecorator` and `AlertDecorator` wrap strategies without touching their code |
| **Factory** | `Sensors/SensorFactory.cs` | Builds the full decorator chain per sensor; callers get a ready-to-use `ISensorStrategy` |
| **Observer** | `Hubs/SensorHub.cs` + JS client | SignalR clients observe the hub; `AlertDecorator` raises events the service observes |
| **Publisher/Subscriber** | `Services/SensorBroadcastService.cs` | `System.Threading.Channels` decouples the sensor polling producer from the SignalR consumer |
| **Background Service** | `Services/SensorBroadcastService.cs` | `IHostedService` lifecycle ties polling to the ASP.NET Core host |

---

### Strategy

**Files:** `Sensors/Strategies/ISensorStrategy.cs`, `Sensors/Strategies/SensorStrategies.cs`

`ISensorStrategy` defines a uniform contract for reading a sensor — `SensorName`, `Unit`, and `ReadAsync()`. Each physical measurement (temperature, humidity, pressure, pitch, roll, yaw) gets its own concrete class that implements this interface independently. Nothing outside of a strategy class needs to know how that sensor works internally.

This makes each sensor independently testable (just mock `ISensorStrategy`) and independently replaceable — swapping a real `TemperatureStrategy` for a simulated one, or adding a new sensor entirely, requires no changes to any other class. The `SensorBroadcastService` polls sensors through the interface and never has to branch on sensor type.

---

### Decorator

**Files:** `Sensors/Decorators/SensorDecorators.cs`

`SensorDecoratorBase` implements `ISensorStrategy` and holds a reference to an inner `ISensorStrategy`. Subclasses wrap the inner strategy's `ReadAsync()` call with additional behaviour, without modifying or subclassing the original strategy.

Two decorators are provided:

- **`LoggingDecorator`** calls the inner strategy, then writes a structured debug log entry for every successful reading. It adds observability without the strategy itself needing a logger.
- **`AlertDecorator`** calls the inner strategy and checks the result against a min/max threshold. If the reading is out of range it fires an `OnAlert` event (with a `Critical` severity if the breach is extreme), then returns the reading unchanged. Threshold checking is layered on top without touching the strategy's code.

Because both decorators implement `ISensorStrategy` themselves, they can be stacked in any order. The factory uses this to build a three-layer chain: `AlertDecorator` → `LoggingDecorator` → concrete strategy.

---

### Factory

**File:** `Sensors/SensorFactory.cs`

`SensorFactory` is the single place responsible for wiring up the decorator chain for each sensor. Private methods like `CreateTemperature()` construct the full stack — concrete strategy wrapped in a `LoggingDecorator` wrapped in an `AlertDecorator` — with the appropriate thresholds for that sensor. `CreateAll()` returns all sensors as `IEnumerable<ISensorStrategy>`.

Callers (specifically `SensorBroadcastService`) receive a ready-to-use `ISensorStrategy` and never need to know that decorators exist, what the thresholds are, or how many layers are involved. Adding a new sensor means adding one `Create…()` method and registering it in `CreateAll()` — nothing else changes.

---

### Observer

**Files:** `Hubs/SensorHub.cs`, `wwwroot/js/dashboard.js`

The Observer pattern appears in two places in this project.

First, `AlertDecorator` exposes an `OnAlert` event (`Action<AlertEvent>?`). In `SensorBroadcastService.ExecuteAsync`, the service iterates over all sensors, finds any that are `AlertDecorator` instances, and subscribes to their `OnAlert` event. When a threshold is breached the decorator fires the event, and the service's handler broadcasts the alert over SignalR. The decorator doesn't know who is listening — it just raises the event.

Second, SignalR itself implements the Observer pattern at the network level. Each browser that opens the dashboard connects to `SensorHub` and becomes an observer of the sensor stream. `SensorBroadcastService` pushes readings to all connected clients via `_hubContext.Clients.All.SendAsync(...)`. The JavaScript client in `dashboard.js` registers handlers (`connection.on("ReceiveReading", ...)`) that update the UI whenever a reading arrives. When a new client connects, `SensorHub.OnConnectedAsync` immediately sends the latest snapshot so the dashboard never starts blank.

---

### Publisher/Subscriber

**File:** `Services/SensorBroadcastService.cs`

`System.Threading.Channels` provides a thread-safe, bounded in-process message bus. `SensorBroadcastService` runs two concurrent tasks: a **producer** (`ProduceAsync`) that polls every sensor each second and writes `SensorReading` values to the channel, and a **consumer** (`ConsumeAsync`) that reads from the channel and forwards each reading to SignalR clients.

The channel acts as a buffer between the two. The producer doesn't call SignalR directly; the consumer doesn't know where readings come from. If the channel fills up (capacity is 64), the oldest reading is dropped rather than blocking the producer — appropriate for a live sensor stream where fresh data is more valuable than old data. The two tasks run concurrently via `Task.WhenAll`, and cancellation of `stoppingToken` (on app shutdown) causes the producer to call `_channel.Writer.Complete()`, which drains the consumer cleanly.

---

### Background Service

**File:** `Services/SensorBroadcastService.cs`, `Program.cs`

`SensorBroadcastService` extends `BackgroundService`, which is ASP.NET Core's base class for long-running hosted services. The framework calls `ExecuteAsync` once at startup on a background thread and passes a `CancellationToken` that is triggered when the application shuts down. The service runs its producer/consumer loop until that token fires, then returns — at which point the framework waits for it to finish before the process exits.

In `Program.cs` the service is registered as a singleton first (`AddSingleton<SensorBroadcastService>()`) so that `SensorHub` can inject it directly to read `LatestSnapshot`. It is then registered as the hosted service via `AddHostedService(sp => sp.GetRequiredService<SensorBroadcastService>())`, which retrieves the same instance rather than creating a second one. This ensures there is exactly one background loop and one snapshot regardless of how many components depend on the service.

---

## Prerequisites

- Raspberry Pi with SenseHat attached
- .NET 8 SDK (`curl -sSL https://dot.net/v1/dotnet-install.sh | bash -s -- --channel 8.0`)
- SenseHat enabled in `/boot/config.txt` (add `dtoverlay=rpi-sense` if needed)

---

## Run

```bash
cd SenseHatDashboard
dotnet run
```

Then open `http://<pi-ip>:5000` from any device on the same network.

---

## Project Structure

```
SenseHatDashboard/
├── Program.cs                          # DI wiring, app startup
├── Models/
│   └── SensorModels.cs                 # SensorReading, DashboardSnapshot, AlertEvent
├── Sensors/
│   ├── SensorFactory.cs                # Factory — builds decorated sensor chains
│   ├── Strategies/
│   │   ├── ISensorStrategy.cs          # Strategy interface
│   │   └── SensorStrategies.cs        # Temperature, Humidity, Pressure, Pitch, Roll, Yaw
│   └── Decorators/
│       └── SensorDecorators.cs        # LoggingDecorator, AlertDecorator
├── Services/
│   └── SensorBroadcastService.cs      # BackgroundService + Channel (pub/sub)
├── Hubs/
│   └── SensorHub.cs                   # SignalR hub (Observer)
├── Pages/
│   ├── Index.cshtml                   # Dashboard HTML
│   └── Index.cshtml.cs
└── wwwroot/
    ├── css/dashboard.css
    └── js/dashboard.js                # SignalR client (Observer, browser side)
```

---

## Adding a New Sensor

1. Create a new class in `Sensors/Strategies/` implementing `ISensorStrategy`
2. Add a `Create<SensorName>()` method in `SensorFactory`
3. Include it in `CreateAll()`
4. Add a card to `Index.cshtml` and handle the sensor name in `dashboard.js`

No other files need to change — that's the Strategy + Factory combo working as intended.

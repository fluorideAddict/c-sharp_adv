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

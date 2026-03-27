# SenseHat Dashboard

ASP.NET Core 8 dashboard served from a Raspberry Pi, streaming live SenseHat
sensor data to any browser on the same network via SignalR.

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

## Adding a New Sensor

1. Create a new class in `Sensors/Strategies/` implementing `ISensorStrategy`
2. Add a `Create<SensorName>()` method in `SensorFactory`
3. Include it in `CreateAll()`
4. Add a card to `Index.cshtml` and handle the sensor name in `dashboard.js`

No other files need to change — that's the Strategy + Factory combo working as intended.

using Iot.Device.SenseHat;
using SenseHatDashboard.Models;

namespace SenseHatDashboard.Sensors.Strategies;

/// <summary>
/// PATTERN: Strategy — Concrete strategy for temperature
/// </summary>
public class TemperatureStrategy : ISensorStrategy
{
    private readonly SenseHat _senseHat;
    public string SensorName => "Temperature";
    public string Unit => "°C";

    public TemperatureStrategy(SenseHat senseHat) => _senseHat = senseHat;

    public Task<SensorReading?> ReadAsync(CancellationToken ct = default)
    {
        var temp = _senseHat.Temperature;
        var reading = new SensorReading(SensorName, Math.Round(temp.DegreesCelsius, 2), Unit, DateTimeOffset.UtcNow);
        return Task.FromResult<SensorReading?>(reading);
    }
}

/// <summary>
/// PATTERN: Strategy — Concrete strategy for humidity
/// </summary>
public class HumidityStrategy : ISensorStrategy
{
    private readonly SenseHat _senseHat;
    public string SensorName => "Humidity";
    public string Unit => "%RH";

    public HumidityStrategy(SenseHat senseHat) => _senseHat = senseHat;

    public Task<SensorReading?> ReadAsync(CancellationToken ct = default)
    {
        var humidity = _senseHat.Humidity;
        var reading = new SensorReading(SensorName, Math.Round(humidity.Percent, 2), Unit, DateTimeOffset.UtcNow);
        return Task.FromResult<SensorReading?>(reading);
    }
}

/// <summary>
/// PATTERN: Strategy — Concrete strategy for barometric pressure
/// </summary>
public class PressureStrategy : ISensorStrategy
{
    private readonly SenseHat _senseHat;
    public string SensorName => "Pressure";
    public string Unit => "hPa";

    public PressureStrategy(SenseHat senseHat) => _senseHat = senseHat;

    public Task<SensorReading?> ReadAsync(CancellationToken ct = default)
    {
        var pressure = _senseHat.Pressure;
        var reading = new SensorReading(SensorName, Math.Round(pressure.Hectopascals, 2), Unit, DateTimeOffset.UtcNow);
        return Task.FromResult<SensorReading?>(reading);
    }
}

/// <summary>
/// PATTERN: Strategy — Concrete strategy for IMU pitch/roll/yaw.
/// The dotnet IoT SenseHat binding exposes sh.Acceleration as a Vector3 (in Gs).
/// Pitch and Roll are derived from that via standard tilt equations.
/// Yaw requires a magnetometer fusion which the binding doesn't expose directly,
/// so we use AngularRate.Z (degrees/sec) as a reasonable proxy.
/// </summary>
public class PitchStrategy : ISensorStrategy
{
    private readonly SenseHat _senseHat;
    public string SensorName => "Pitch";
    public string Unit => "°";

    public PitchStrategy(SenseHat senseHat) => _senseHat = senseHat;

    public Task<SensorReading?> ReadAsync(CancellationToken ct = default)
    {
        var a = _senseHat.Acceleration; // Vector3 in Gs
        var pitch = Math.Atan2(-a.X, Math.Sqrt(a.Y * a.Y + a.Z * a.Z)) * (180.0 / Math.PI);
        var reading = new SensorReading(SensorName, Math.Round(pitch, 2), Unit, DateTimeOffset.UtcNow);
        return Task.FromResult<SensorReading?>(reading);
    }
}

public class RollStrategy : ISensorStrategy
{
    private readonly SenseHat _senseHat;
    public string SensorName => "Roll";
    public string Unit => "°";

    public RollStrategy(SenseHat senseHat) => _senseHat = senseHat;

    public Task<SensorReading?> ReadAsync(CancellationToken ct = default)
    {
        var a = _senseHat.Acceleration;
        var roll = Math.Atan2(a.Y, a.Z) * (180.0 / Math.PI);
        var reading = new SensorReading(SensorName, Math.Round(roll, 2), Unit, DateTimeOffset.UtcNow);
        return Task.FromResult<SensorReading?>(reading);
    }
}

public class YawStrategy : ISensorStrategy
{
    private readonly SenseHat _senseHat;
    public string SensorName => "Yaw";
    public string Unit => "°/s";

    public YawStrategy(SenseHat senseHat) => _senseHat = senseHat;

    public Task<SensorReading?> ReadAsync(CancellationToken ct = default)
    {
        // AngularRate is a Vector3 in degrees/sec; Z axis = yaw rate
        var yawRate = _senseHat.AngularRate.Z;
        var reading = new SensorReading(SensorName, Math.Round(yawRate, 2), Unit, DateTimeOffset.UtcNow);
        return Task.FromResult<SensorReading?>(reading);
    }
}

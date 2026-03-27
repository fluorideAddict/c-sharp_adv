using SenseHatDashboard.Models;
using SenseHatDashboard.Sensors.Strategies;

namespace SenseHatDashboard.Sensors.Decorators;

/// <summary>
/// PATTERN: Decorator (base)
/// Wraps an ISensorStrategy and forwards calls, allowing subclasses
/// to add behaviour before/after without touching the wrapped strategy.
/// </summary>
public abstract class SensorDecoratorBase : ISensorStrategy
{
    protected readonly ISensorStrategy _inner;

    protected SensorDecoratorBase(ISensorStrategy inner) => _inner = inner;

    public virtual string SensorName => _inner.SensorName;
    public virtual string Unit => _inner.Unit;
    public abstract Task<SensorReading?> ReadAsync(CancellationToken ct = default);
}

/// <summary>
/// PATTERN: Decorator — adds structured logging around every read
/// </summary>
public class LoggingDecorator : SensorDecoratorBase
{
    private readonly ILogger _logger;

    public LoggingDecorator(ISensorStrategy inner, ILogger logger)
        : base(inner) => _logger = logger;

    public override async Task<SensorReading?> ReadAsync(CancellationToken ct = default)
    {
        var reading = await _inner.ReadAsync(ct);
        if (reading is not null)
            _logger.LogDebug("[{Sensor}] {Value} {Unit} at {Time}",
                reading.SensorName, reading.Value, reading.Unit, reading.Timestamp);
        return reading;
    }
}

/// <summary>
/// PATTERN: Decorator — raises an alert event when a reading is out of range.
/// Consumers can subscribe to OnAlert to receive threshold breach notifications.
/// </summary>
public class AlertDecorator : SensorDecoratorBase
{
    private readonly double _min;
    private readonly double _max;

    public event Action<AlertEvent>? OnAlert;

    public AlertDecorator(ISensorStrategy inner, double minThreshold, double maxThreshold, string unit)
        : base(inner)
    {
        _min = minThreshold;
        _max = maxThreshold;
    }

    public override async Task<SensorReading?> ReadAsync(CancellationToken ct = default)
    {
        var reading = await _inner.ReadAsync(ct);
        if (reading is null) return null;

        if (reading.Value < _min || reading.Value > _max)
        {
            var severity = IsExtreme(reading.Value) ? AlertSeverity.Critical : AlertSeverity.Warning;
            OnAlert?.Invoke(new AlertEvent(
                reading.SensorName,
                $"{reading.SensorName} out of range: {reading.Value} {reading.Unit}",
                severity,
                DateTimeOffset.UtcNow));
        }

        return reading;
    }

    private bool IsExtreme(double value) =>
        value < _min * 1.2 || value > _max * 1.2;
}

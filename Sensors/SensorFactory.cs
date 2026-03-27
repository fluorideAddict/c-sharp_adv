using Iot.Device.SenseHat;
using SenseHatDashboard.Sensors.Decorators;
using SenseHatDashboard.Sensors.Strategies;

namespace SenseHatDashboard.Sensors;

/// <summary>
/// PATTERN: Factory
/// Creates fully-decorated sensor strategies. The caller doesn't need to know
/// about the decorator chain — just ask for a sensor by name.
/// </summary>
public class SensorFactory
{
    private readonly SenseHat _senseHat;
    private readonly ILogger<SensorFactory> _logger;

    public SensorFactory(SenseHat senseHat, ILogger<SensorFactory> logger)
    {
        _senseHat = senseHat;
        _logger = logger;
    }

    public IEnumerable<ISensorStrategy> CreateAll()
    {
        return new[]
        {
            CreateTemperature(),
            CreateHumidity(),
            CreatePressure(),
            CreatePitch(),
            CreateRoll(),
            CreateYaw(),
        };
    }

    // Each sensor gets decorated with logging, then threshold alerting.
    // PATTERN: Decorator chain — outer wrappers add behaviour without touching inner classes.

    private ISensorStrategy CreateTemperature() =>
        new AlertDecorator(
            new LoggingDecorator(
                new TemperatureStrategy(_senseHat),
                _logger),
            minThreshold: -10, maxThreshold: 50,
            unit: "°C");

    private ISensorStrategy CreateHumidity() =>
        new AlertDecorator(
            new LoggingDecorator(
                new HumidityStrategy(_senseHat),
                _logger),
            minThreshold: 0, maxThreshold: 95,
            unit: "%RH");

    private ISensorStrategy CreatePressure() =>
        new AlertDecorator(
            new LoggingDecorator(
                new PressureStrategy(_senseHat),
                _logger),
            minThreshold: 870, maxThreshold: 1085,
            unit: "hPa");

    private ISensorStrategy CreatePitch() =>
        new LoggingDecorator(new PitchStrategy(_senseHat), _logger);

    private ISensorStrategy CreateRoll() =>
        new LoggingDecorator(new RollStrategy(_senseHat), _logger);

    private ISensorStrategy CreateYaw() =>
        new LoggingDecorator(new YawStrategy(_senseHat), _logger);
}

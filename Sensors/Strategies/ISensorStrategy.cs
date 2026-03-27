using SenseHatDashboard.Models;

namespace SenseHatDashboard.Sensors.Strategies;

/// <summary>
/// PATTERN: Strategy
/// Defines a common interface for all sensor reading strategies.
/// Each sensor gets its own concrete strategy, swappable at runtime.
/// </summary>
public interface ISensorStrategy
{
    string SensorName { get; }
    string Unit { get; }
    Task<SensorReading?> ReadAsync(CancellationToken ct = default);
}

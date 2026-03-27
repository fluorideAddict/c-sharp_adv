namespace SenseHatDashboard.Models;

public record SensorReading(
    string SensorName,
    double Value,
    string Unit,
    DateTimeOffset Timestamp
);

public record DashboardSnapshot(
    SensorReading? Temperature,
    SensorReading? Humidity,
    SensorReading? Pressure,
    SensorReading? Pitch,
    SensorReading? Roll,
    SensorReading? Yaw
);

public record AlertEvent(
    string SensorName,
    string Message,
    AlertSeverity Severity,
    DateTimeOffset Timestamp
);

public enum AlertSeverity { Info, Warning, Critical }

using Iot.Device.SenseHat;
using SenseHatDashboard.Hubs;
using SenseHatDashboard.Sensors;
using SenseHatDashboard.Services;

var builder = WebApplication.CreateBuilder(args);

// Register SenseHat as a singleton (one physical device)
builder.Services.AddSingleton(_ => new SenseHat());

// PATTERN: Factory — registered so DI can inject it anywhere
builder.Services.AddSingleton<SensorFactory>();

// PATTERN: Background Service — DI manages lifecycle
builder.Services.AddSingleton<SensorBroadcastService>();
builder.Services.AddHostedService(sp => sp.GetRequiredService<SensorBroadcastService>());

builder.Services.AddSignalR();
builder.Services.AddRazorPages();

var app = builder.Build();

app.UseStaticFiles();
app.UseRouting();
app.MapRazorPages();
app.MapHub<SensorHub>("/sensorHub");

app.Run("http://0.0.0.0:5000"); // Listen on all interfaces so LAN clients can connect

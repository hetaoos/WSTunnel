
using WSTunnel;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddWSTunnel();
// Add services to the container.

var app = builder.Build();

var webSocketOptions = new WebSocketOptions
{
    KeepAliveInterval = TimeSpan.FromMinutes(2)
};

app.UseWebSockets(webSocketOptions);
app.MapGet("/ws", WSTunnelRequestDelegate.Request);

app.Run();

internal record WeatherForecast(DateTime Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}
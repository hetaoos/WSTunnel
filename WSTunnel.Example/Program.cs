
using WSTunnel;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddWSTunnel();
// Add services to the container.

var app = builder.Build();

app.UseWebSockets();
app.MapGet("/ws", WSTunnelRequestDelegate.Request);

app.Run();
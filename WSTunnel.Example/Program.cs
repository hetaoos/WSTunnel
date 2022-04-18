// 添加命名空间
using WSTunnel;

var builder = WebApplication.CreateBuilder(args);

//添加隧道服务
builder.Services.AddWSTunnel();
// Add services to the container.

var app = builder.Build();

//开启 WebSocket 支持
app.UseWebSockets();

// 修改参数加密密码，用于认证。建议修改下。
// TunnelParam.param_key = System.Text.Encoding.UTF8.GetBytes("azleKxOgDmp4wV7l");

//绑定 WebSocket 路径，目前为 /ws，建议修改为其他。
app.MapGet("/ws", WSTunnelRequestDelegate.Request);

app.Run();
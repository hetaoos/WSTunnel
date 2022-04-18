# WSTunnel
运行在 asp.net core 上，基于 WebSocket 的隧道

# 使用
## 服务端配置
### 引用包
```
dotnet add package WSTunnel
```
### 配置服务
```csharpe
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
```

## 客户端运行参数
```
wsclient.exe --help
Description:
  WebSocket 隧道客户端

Usage:
  WSClient [options]

Options:
  -s, --server <server> (REQUIRED)  目标服务器地址。
                                    示例：wss://myserver.com/ws/mqtt
  -t, --tunnel <tunnel> (REQUIRED)  隧道配置，允许配置多个。
                                    格式：[bind_address:]bind_port:target_server:target_port
                                    示例：0.0.0.0:12345:github.com:22
                                    等效于: 12345:github.com:22
                                         bind_address：127.0.0.1 监听地址，为空默认为 0.0.0.0。
                                         bind_port: 12345 监听端口。
                                         target_server： github.com 目标服务器地址。
                                         target_port： 22 目标服务器端口。
  -k, --key <key>                   认证密钥，长度是16个字符。
  --version                         Show version information
  -?, -h, --help                    Show help and usage information
```

使用示例：
```
wsclient.exe --server ws://localhost:5111/ws --tunnel 12345:github.com:222
```
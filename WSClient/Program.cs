// See https://aka.ms/new-console-template for more information
using System.CommandLine;
using System.Net;
using System.Text;
using WSTunnel;

namespace WSClient;

internal class Program
{
    private static async Task<int> Main(string[] args)
    {
        var serverOption = new Option<string>("--server", "目标服务器地址。\n示例：wss://myserver.com/ws/mqtt") { IsRequired = true, Arity = ArgumentArity.ExactlyOne };
        serverOption.AddAlias("-s");
        var tunnelOption = new Option<string[]>("--tunnel", @"隧道配置，允许配置多个。
格式：[bind_address:]bind_port:target_server:target_port
示例：0.0.0.0:12345:github.com:22
等效于: 12345:github.com:22
     bind_address：127.0.0.1 监听地址，为空默认为 0.0.0.0。
     bind_port: 12345 监听端口。
     target_server： github.com 目标服务器地址。
     target_port： 22 目标服务器端口。") { IsRequired = true, Arity = ArgumentArity.OneOrMore };
        tunnelOption.AddAlias("-t");

        var keyOption = new Option<string>("--key", "认证密钥，长度是16个字符。");
        keyOption.AddAlias("-k");

        var rootCommand = new RootCommand("WebSocket 隧道客户端");
        rootCommand.AddOption(serverOption);
        rootCommand.AddOption(tunnelOption);
        rootCommand.AddOption(keyOption);

        rootCommand.SetHandler(async (string server, string[] tunnels, string? key) =>
        {
            if (string.IsNullOrEmpty(server) || !(server.StartsWith("ws://", StringComparison.CurrentCultureIgnoreCase) || server.StartsWith("wss://", StringComparison.CurrentCultureIgnoreCase)))
            {
                Console.WriteLine($"--server {server} 参数不正确，必须是 wss:// 或者 ws:// 开头");
                return;
            }
            else
            {
                try
                {
                    TunnelParam.server = new Uri(server);
                }
                catch
                {
                    Console.WriteLine($"--server {server} 参数不正确");
                    return;
                }
            }
            if (!string.IsNullOrWhiteSpace(key))
            {
                var k2 = Encoding.UTF8.GetBytes(key);
                if (k2.Length != 16)
                {
                    Console.WriteLine($"--key {key} 长度不正确，UTF8转换后必须是16个字节，当前是 {k2.Length} 字节。");
                    return;
                }
                TunnelParam.param_key = k2;
            }
            else
                key = null;

            var connections = new List<TunnelParam>();
            foreach (var tunnel in tunnels)
            {
                var values = tunnel.Split(':');
                if (values.Length < 3 || values.Length > 4)
                {
                    Console.WriteLine($"--tunnel {tunnel} 参数不正确。");
                    return;
                }
                var connection = new TunnelParam();
                int index = 0;
                if (values.Length == 4)
                {
                    var bind_host = values[index++];
                    if (IPAddress.TryParse(bind_host, out var ip))
                    {
                        connection.bind_host = ip;
                    }
                    else
                    {
                        var host = await Dns.GetHostEntryAsync(bind_host);
                        if (host == null || host.AddressList?.Any() != true)
                        {
                            Console.WriteLine($"--tunnel {tunnel} {connection.bind_host} 不正确。");
                            return;
                        }
                        connection.bind_host = host.AddressList[0];
                    }
                }
                if (!int.TryParse(values[index++], out var port) || port <= 0 || port >= ushort.MaxValue)
                {
                    Console.WriteLine($"--tunnel {tunnel} 本地监听端口不正确。");
                    return;
                }
                connection.bind_port = port;
                connection.host = values[index++];
                if (string.IsNullOrEmpty(connection.host))
                {
                    Console.WriteLine($"--tunnel {tunnel} 目标主机不正确。");
                    return;
                }
                if (!int.TryParse(values[index++], out port) || port <= 0 || port >= short.MaxValue)
                {
                    Console.WriteLine($"--tunnel {tunnel} 目标端口不正确。");
                    return;
                }
                connection.port = port;
                connection.key = TunnelParam.GetRandomKey();
                connections.Add(connection);
            }
            Console.WriteLine($"server: {server}");
            var workers = connections.Select(o => new TunnelWorker(o)).ToArray();
            var tasks = workers.Select(o => o.RunAsync(default)).ToArray();
            await Task.WhenAll(tasks);
        }, serverOption, tunnelOption, keyOption);

        return await rootCommand.InvokeAsync(args);
    }
}
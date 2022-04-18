using DotNetty.Handlers.Logging;
using DotNetty.Transport.Bootstrapping;
using DotNetty.Transport.Channels;
using DotNetty.Transport.Channels.Sockets;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Net.WebSockets;
using System.Threading.Tasks;

namespace WSTunnel
{
    /// <summary>
    /// 客户端服务
    /// </summary>
    public class TunnelService
    {
        private readonly IServiceProvider serviceProvider;
        private readonly ILoggerFactory loggerFactory;
        private readonly ILogger<TunnelService> log;
        private Bootstrap? bootstrap;

        /// <summary>
        ///
        /// </summary>
        /// <param name="serviceProvider"></param>
        /// <param name="loggerFactory"></param>
        /// <param name="log"></param>
        public TunnelService(IServiceProvider serviceProvider,
            ILoggerFactory loggerFactory,
            ILogger<TunnelService> log)
        {
            this.serviceProvider = serviceProvider;
            this.loggerFactory = loggerFactory;
            this.log = log;
            InitializeTcpSocketChannel();
        }

        /// <summary>
        /// 初始化网络回复
        /// </summary>
        protected void InitializeTcpSocketChannel()
        {
            var bossGroup = new MultithreadEventLoopGroup();
            var logClientHandler = loggerFactory.CreateLogger<TunnelHandler>();
            try
            {
                bootstrap = new Bootstrap();
                bootstrap.Group(bossGroup);
                bootstrap.Channel<TcpSocketChannel>();

                bootstrap
                    .Option(ChannelOption.TcpNodelay, true)
                    .Handler(new LoggingHandler("SRV-LSTN"))
                    .Handler(new ActionChannelInitializer<ISocketChannel>(channel =>
                    {
                        IChannelPipeline pipeline = channel.Pipeline;
                        pipeline.AddLast(new LoggingHandler("SRV-CONN"));
                        pipeline.AddLast("echo", serviceProvider.GetService<TunnelHandler>());
                    }));
            }
            catch
            {
                bossGroup.ShutdownGracefullyAsync().Wait(1000);
                bootstrap = null;
            }
        }

        /// <summary>
        /// 开始交互
        /// </summary>
        /// <param name="webSocket"></param>
        /// <param name="param"></param>
        /// <returns></returns>
        public async Task<bool> Handle(WebSocket webSocket, TunnelParam param)
        {
            if (bootstrap == null)
                return false;
            IChannel? channel = null;
            try
            {
                channel = await bootstrap.ConnectAsync(param.host, param.port);
                var handler = channel.Pipeline.LastOrDefault(o => o is TunnelHandler) as TunnelHandler;
                if (handler != null)
                {
                    await handler.Handle(webSocket, param);
                    return true;
                }
            }
            catch
            {
                try
                {
                    if (channel != null)
                        await channel.CloseAsync();
                }
                catch { }
            }

            return false;
        }
    }
}
using DotNetty.Handlers.Logging;
using DotNetty.Transport.Bootstrapping;
using DotNetty.Transport.Channels;
using DotNetty.Transport.Channels.Sockets;
using System.Net;

namespace WSTunnel
{
    /// <summary>
    /// 隧道工
    /// </summary>
    public class TunnelWorker
    {
        private readonly TunnelParam param;

        public TunnelWorker(TunnelParam param)
        {
            this.param = param;
        }

        public async Task RunAsync(CancellationToken stoppingToken)
        {
            IEventLoopGroup bossGroup;
            IEventLoopGroup workerGroup;
            bossGroup = new MultithreadEventLoopGroup();
            workerGroup = new MultithreadEventLoopGroup();

            try
            {
                var bootstrap = new ServerBootstrap();
                bootstrap.Group(bossGroup, workerGroup);

                bootstrap.Channel<TcpServerSocketChannel>();

                bootstrap
                    .Option(ChannelOption.SoBacklog, 100)
                    .Handler(new LoggingHandler("SRV-LSTN"))
                    .ChildHandler(new ActionChannelInitializer<IChannel>(channel =>
                    {
                        IChannelPipeline pipeline = channel.Pipeline;
                        pipeline.AddLast(new LoggingHandler("SRV-CONN"));
                        pipeline.AddLast("echo", new TunnelHandler(param));
                    }));

                var ip = new IPEndPoint(param.bind_host, param.bind_port);
                param.InitializeEncryptionService();
                IChannel boundChannel = await bootstrap.BindAsync(ip);
                Console.WriteLine($"tunnel: {ip} --> {param.host}:{param.port}");
                await stoppingToken.WhenCanceled();
                await boundChannel.CloseAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
            finally
            {
                await Task.WhenAll(
                    bossGroup.ShutdownGracefullyAsync(TimeSpan.FromMilliseconds(100), TimeSpan.FromSeconds(1)),
                    workerGroup.ShutdownGracefullyAsync(TimeSpan.FromMilliseconds(100), TimeSpan.FromSeconds(1)));
                param.DestroyEncryptionService();
            }
        }
    }

    /// <summary>
    /// Task 扩展
    /// </summary>
    public static class TaskExtensions
    {
        /// <summary>
        /// 等待取消
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public static Task WhenCanceled(this CancellationToken cancellationToken)
        {
            var tcs = new TaskCompletionSource<bool>();
            cancellationToken.Register(s => ((TaskCompletionSource<bool>)s).SetResult(true), tcs);
            return tcs.Task;
        }
    }
}
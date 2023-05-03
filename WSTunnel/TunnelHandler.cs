using DotNetty.Buffers;
using DotNetty.Transport.Channels;
using Microsoft.Extensions.Logging;
using System;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;

namespace WSTunnel
{
    /// <summary>
    /// 客户端
    /// </summary>
    public class TunnelHandler : SimpleChannelInboundHandler<IByteBuffer>
    {
        private IChannelHandlerContext? ctx = null;
        private readonly IServiceProvider serviceProvider;
        private readonly ILogger<TunnelHandler> log;
        private WebSocket? webSocket;
        private TunnelParam? param;
        private CancellationTokenSource? cts;
        private CancellationToken cancellationToken = CancellationToken.None;

        /// <summary>
        ///
        /// </summary>
        /// <param name="serviceProvider"></param>
        /// <param name="clientSettings"></param>
        /// <param name="storageService"></param>
        /// <param name="log"></param>
        public TunnelHandler(IServiceProvider serviceProvider, ILogger<TunnelHandler> log)
        {
            this.serviceProvider = serviceProvider;
            this.log = log;
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="ctx"></param>
        public override void ChannelReadComplete(IChannelHandlerContext ctx)
            => ctx.Flush();

        /// <summary>
        ///
        /// </summary>
        /// <param name="ctx"></param>
        /// <param name="exception"></param>
        public override void ExceptionCaught(IChannelHandlerContext ctx, Exception exception)
        {
            log.LogError($"{ctx.Channel.Id} {exception}");
            ctx.CloseAsync();
            cts?.Cancel();
        }

        /// <summary>
        /// 通道激活
        /// </summary>
        /// <param name="context"></param>
        public override void ChannelActive(IChannelHandlerContext context)
        {
            ctx = context;
            base.ChannelActive(context);
        }

        /// <summary>
        /// 通道关闭
        /// </summary>
        /// <param name="context"></param>
        public override void ChannelInactive(IChannelHandlerContext context)
        {
            cts?.Cancel();
            cts = null;
            base.ChannelInactive(context);
            this.ctx = null;
            webSocket = null;
        }

        protected override void ChannelRead0(IChannelHandlerContext ctx, IByteBuffer msg)
        {
            //尚未初始化完成
            if (webSocket == null || param == null)
            {
                ctx.CloseAsync();
                return;
            }
            if (msg.ReadableBytes == 0)
                return;
            var len = msg.ReadableBytes;
            var bytes = new byte[len];
            msg.ReadBytes(bytes, 0, len);
            var ee = param.Encrypt(bytes, 0, bytes.Length);
            var task = webSocket.SendAsync(ee, WebSocketMessageType.Binary, true, cancellationToken);
            task.ConfigureAwait(false).GetAwaiter().GetResult();
            //Console.WriteLine($"======================= SendAsync: {msg.ReaderIndex} {len}\n{Encoding.UTF8.GetString(bytes)}");
        }

        /// <summary>
        /// 开始处理
        /// </summary>
        /// <param name="webSocket"></param>
        /// <param name="param"></param>
        /// <returns></returns>
        public async Task Handle(WebSocket webSocket, TunnelParam param)
        {
            this.webSocket = webSocket;
            this.param = param;
            param.InitializeEncryptionService();

            cts = new CancellationTokenSource();
            cancellationToken = cts.Token;

            var buffer = new byte[1024 * 1024];
            var receiveResult = await webSocket.ReceiveAsync(
                new ArraySegment<byte>(buffer), cancellationToken);

            while (!(cancellationToken.IsCancellationRequested || receiveResult.CloseStatus.HasValue))
            {
                if (receiveResult.Count > 0)
                {
                    //解密
                    var dd = param.Decrypt(buffer, 0, receiveResult.Count);
                    var bfw = Unpooled.Buffer();
                    bfw.WriteBytes(dd, 0, dd.Length);
                    await ctx.WriteAndFlushAsync(bfw);
                    //Console.WriteLine($"======================= ReceiveAsync: {dd.Length}: \n{Encoding.UTF8.GetString(buffer, 0, receiveResult.Count)}");
                }

                receiveResult = await webSocket.ReceiveAsync(
                    new ArraySegment<byte>(buffer), cancellationToken);
            }

            if (webSocket != null && webSocket.CloseStatus == null)
                await webSocket.CloseOutputAsync(
                     WebSocketCloseStatus.NormalClosure,
                    receiveResult.CloseStatusDescription,
                    CancellationToken.None);
            param.DestroyEncryptionService();
        }
    }
}
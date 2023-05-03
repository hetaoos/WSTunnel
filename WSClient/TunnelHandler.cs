using DotNetty.Buffers;
using DotNetty.Transport.Channels;
using System.Net.WebSockets;

namespace WSTunnel
{
    /// <summary>
    /// 包处理
    /// </summary>
    public class TunnelHandler : SimpleChannelInboundHandler<IByteBuffer>
    {
        private IChannelHandlerContext? ctx = null;
        private TunnelParam param;
        private ClientWebSocket? client;

        public TunnelHandler(TunnelParam param)
        {
            this.param = param;
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
            Console.WriteLine($"{ctx.Channel.Id} {exception?.ToString()}");
            ctx.CloseAsync();
        }

        /// <summary>
        /// 通道激活
        /// </summary>
        /// <param name="context"></param>
        public override void ChannelActive(IChannelHandlerContext context)
        {
            ctx = context;
            base.ChannelActive(context);
            Console.WriteLine($"New Connection: {ctx.Channel.Id} {context.Channel.RemoteAddress} --> {param.host}:{param.port}");
            client = new ClientWebSocket();
            param.time = DateTime.Now;
            var token = param.ToAccessToken();
            client.Options.SetRequestHeader("Authorization", token);
            client.ConnectAsync(TunnelParam.server, default)
                 .ConfigureAwait(false).GetAwaiter().GetResult();
            var task = Task.Run(ConnectAsync);
        }

        private async Task ConnectAsync()
        {
            var buffer = new byte[1024 * 1024];

            while (client?.State == WebSocketState.Open)
            {
                var receiveResult = await client.ReceiveAsync(
                    new ArraySegment<byte>(buffer), default);

                if (receiveResult.Count > 0)
                {
                    var bfw = Unpooled.Buffer();
                    //解密
                    var dd = param.Decrypt(buffer, 0, receiveResult.Count);
                    bfw.WriteBytes(dd, 0, dd.Length);
                    await ctx.WriteAndFlushAsync(bfw);
                    //Console.WriteLine($"======================= ReceiveAsync {receiveResult.Count}: \n{BitConverter.ToString(buffer, 0, receiveResult.Count).Replace('-', ' ')}");
                }
            }
            Console.WriteLine($"Connection Close: {ctx?.Channel.Id} {ctx?.Channel.RemoteAddress} --> {param.host}:{param.port}");

            client?.Dispose();
            client = null;
            ctx?.CloseAsync();
        }

        /// <summary>
        /// 通道关闭
        /// </summary>
        /// <param name="context"></param>
        public override void ChannelInactive(IChannelHandlerContext context)
        {
            if (client != null)
            {
                client?.CloseOutputAsync(WebSocketCloseStatus.NormalClosure, null, default);
                client?.Dispose();
                client = null;
            }
            base.ChannelInactive(context);
            Console.WriteLine($"Connection Inactive: {context?.Channel.Id} {context?.Channel.RemoteAddress} --> {param.host}:{param.port}");
            this.ctx = null;
        }

        /// <summary>
        /// 接收到一个包
        /// </summary>
        /// <param name="ctx">上下文</param>
        /// <param name="text">文本内容</param>
        protected override void ChannelRead0(IChannelHandlerContext ctx, IByteBuffer msg)
        {
            this.ctx = ctx;

            if (client?.State != WebSocketState.Open)
            {
                Console.WriteLine($"Connection Error: {ctx?.Channel.Id} {ctx?.Channel.RemoteAddress} --> {param.host}:{param.port}");
                return;
            }

            var len = msg.ReadableBytes;
            var bytes = new byte[len];
            msg.ReadBytes(bytes, 0, len);
            var ee = param.Encrypt(bytes, 0, bytes.Length);
            var task = client.SendAsync(ee, WebSocketMessageType.Binary, true, default);
            task.ConfigureAwait(false).GetAwaiter().GetResult();
            //Console.WriteLine($"======================= SendAsync {msg.ReaderIndex} {len}: \n{BitConverter.ToString(bytes).Replace('-', ' ')}");
        }
    }
}
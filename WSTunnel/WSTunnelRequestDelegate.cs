using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Linq;
using System.Net.WebSockets;
using System.Threading.Tasks;

namespace WSTunnel
{
    /// <summary>
    /// WebSocket 请求处理
    /// </summary>
    public class WSTunnelRequestDelegate
    {
        /// <summary>
        /// WebSocket 请求处理
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        public static async Task Request(HttpContext context)
        {
            if (!context.WebSockets.IsWebSocketRequest)
            {
                context.Response.StatusCode = StatusCodes.Status400BadRequest;
                return;
            }

            context.Request.Headers.TryGetValue("Authorization", out var hh);
            var access_token = hh.ToString();
            if (string.IsNullOrEmpty(access_token))
            {
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                return;
            }

            TunnelParam? param = null;
            try
            {
                param = TunnelParam.ParseAccessToken(access_token);
            }
            catch { }
            if (param == null || Math.Abs((DateTime.Now - param.time).TotalSeconds) > 120)
            {
                context.Response.StatusCode = StatusCodes.Status400BadRequest;
                return;
            }

            using var webSocket = await context.WebSockets.AcceptWebSocketAsync();
            var tcpClientService = context.Request.HttpContext.RequestServices.GetRequiredService<TunnelService>();
            var r = await tcpClientService.Handle(webSocket, param);
            if (webSocket.CloseStatus == null)
                await webSocket.CloseAsync(r ? WebSocketCloseStatus.NormalClosure : WebSocketCloseStatus.InternalServerError, null, default);
        }
    }
}
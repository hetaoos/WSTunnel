using Microsoft.Extensions.DependencyInjection;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace WSTunnel
{
    /// <summary>
    /// 扩展
    /// </summary>
    public static class WSTunnelExtensions
    {
        /// <summary>
        /// 注册 WSTunnel
        /// </summary>
        /// <param name="services">服务集合</param>
        /// <returns></returns>
        public static IServiceCollection AddWSTunnel(this IServiceCollection services)
        {
            services.AddSingleton<TunnelService>();
            services.AddTransient<TunnelHandler>();
            return services;
        }
    }
}
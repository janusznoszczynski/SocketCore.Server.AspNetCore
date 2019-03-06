using Microsoft.AspNetCore.Builder;

namespace SocketCore.Server.AspNetCore
{
    public static class WebSocketsMiddlewareExtensions
    {
        public static IApplicationBuilder UseSocketCore(this IApplicationBuilder builder, string url, Connection conn)
        {
            builder.UseWebSockets();

            return builder.UseMiddleware<WebSocketsMiddleware>(new WebSocketsMiddlewareOptions()
            {
                Path = url,
                Connection = conn
            });
        }

        public static IApplicationBuilder UseSocketCore(this IApplicationBuilder builder, WebSocketsMiddlewareOptions options)
        {
            builder.UseWebSockets();
            return builder.UseMiddleware<WebSocketsMiddleware>(options);
        }
    }
}
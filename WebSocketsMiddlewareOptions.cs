namespace SocketCore.Server.AspNetCore
{
    public class WebSocketsMiddlewareOptions
    {
        public string Path { get; set; }
        public Connection Connection { get; set; }
    }
}
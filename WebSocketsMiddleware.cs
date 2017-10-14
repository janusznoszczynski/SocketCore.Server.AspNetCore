using System;
using System.Collections.Concurrent;
using System.Net.WebSockets;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System.Linq;

namespace SocketCore.Server.AspNetCore
{
    public class WebSocketsMiddleware
    {
        private readonly RequestDelegate _Next;
        private readonly ILogger _Logger;
        private readonly IConnectionManager _ConnectionManager;
        private readonly string _Path;
        private readonly Connection _Connection;

        public WebSocketsMiddleware(RequestDelegate next, ILoggerFactory loggerFactory, IConnectionManager connectionManager, WebSocketsMiddlewareOptions options)
        {
            _Next = next;
            _Logger = loggerFactory.CreateLogger<WebSocketsMiddleware>();
            _ConnectionManager = connectionManager;
            _Path = options.Path;
            _Connection = options.Connection;
            _Connection.SetConnectionManager(connectionManager);
        }

        public async Task Invoke(HttpContext context)
        {
            if (context.WebSockets.IsWebSocketRequest && PathMatches(context.Request))
            {
                var webSocket = await context.WebSockets.AcceptWebSocketAsync();
                var connectionId = Guid.NewGuid().ToString();

                await webSocket.SendCommandAsync("SetConnectionId", connectionId);

                var handle = await _ConnectionManager.ConnectAsync(connectionId, data =>
                {
                    var str = JsonConvert.SerializeObject(new Command("Data", data));
                    return webSocket.SendTextAsync(str);
                });

                await _Connection.Connected(connectionId);

                while (webSocket.State == WebSocketState.Open)
                {
                    var received = await webSocket.RecieveTextAsync();

                    if (received != null)
                    {
                        try
                        {
                            var cmd = JsonConvert.DeserializeObject<Command>(received);

                            if (cmd.Type == "Data")
                            {
                                await _Connection.Recieved(connectionId, cmd.Data);
                                _Logger.LogInformation($"Recieved '{cmd.Data}' from: {connectionId}");
                            }
                        }
                        catch (Exception ex)
                        {
                            _Logger.LogError(ex.ToString());
                        }
                    }
                }

                await _ConnectionManager.DisconnectAsync(connectionId, handle);
            }
            else
            {
                await _Next.Invoke(context);
            }
        }

        private bool PathMatches(HttpRequest request)
        {
            return request.Path == _Path;
        }

        private ArraySegment<byte> ToArraySegment(string str)
        {
            var data = Encoding.UTF8.GetBytes(str);
            var buffer = new ArraySegment<Byte>(data);
            return buffer;
        }
    }
}
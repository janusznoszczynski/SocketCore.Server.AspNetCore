using System;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace SocketCore.Server.AspNetCore
{
    public static class WebSocketExtensions
    {
        public static Task SendTextAsync(this WebSocket webSocket, string text)
        {
            var data = Encoding.UTF8.GetBytes(text);
            var buffer = new ArraySegment<Byte>(data);
            return webSocket.SendAsync(buffer, WebSocketMessageType.Text, true, CancellationToken.None);
        }

        public static Task SendCommandAsync(this WebSocket webSocket, string type, object data)
        {
            var text = JsonConvert.SerializeObject(new Command(type, data));
            var bytes = Encoding.UTF8.GetBytes(text);
            var buffer = new ArraySegment<Byte>(bytes);
            return webSocket.SendAsync(buffer, WebSocketMessageType.Text, true, CancellationToken.None);
        }

        public static async Task<string> RecieveTextAsync(this WebSocket webSocket)
        {
            var buffer = new ArraySegment<Byte>(new Byte[4096]);
            var received = await webSocket.ReceiveAsync(buffer, CancellationToken.None);

            if (received.MessageType == WebSocketMessageType.Text)
            {
                return Encoding.UTF8.GetString(buffer.Array, 0, received.Count);
            }

            return null;
        }
    }
}
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

        public static Task SendDataAsync(this WebSocket webSocket, object data)
        {
            var text = JsonConvert.SerializeObject(new Command("Data", data));
            var bytes = Encoding.UTF8.GetBytes(text);
            var buffer = new ArraySegment<Byte>(bytes);
            return webSocket.SendAsync(buffer, WebSocketMessageType.Text, true, CancellationToken.None);
        }

        public static async Task<Command> RecieveCommandAsync(this WebSocket webSocket)
        {
            var text = await webSocket.RecieveTextAsync();
            var cmd = JsonConvert.DeserializeObject<Command>(text);
            return cmd;
        }

        public static async Task<object> RecieveDataAsync(this WebSocket webSocket)
        {
            var text = await webSocket.RecieveTextAsync();
            var cmd = JsonConvert.DeserializeObject<Command>(text);

            if (cmd.Type == "Data")
            {
                return cmd.Data;
            }

            return null;
        }

        public static async Task<string> RecieveTextAsync(this WebSocket webSocket)
        {
            var buffer = new ArraySegment<Byte>(new Byte[4096 * 4]);
            var received = await webSocket.ReceiveAsync(buffer, CancellationToken.None);

            var sb = new StringBuilder();

            if (received.MessageType == WebSocketMessageType.Text)
            {
                sb.Append(Encoding.UTF8.GetString(buffer.Array, 0, received.Count));

                do
                {
                    received = await webSocket.ReceiveAsync(buffer, CancellationToken.None);
                    sb.Append(Encoding.UTF8.GetString(buffer.Array, 0, received.Count));
                }
                while (!received.EndOfMessage);

                return sb.ToString();
            }

            return null;
        }
    }
}
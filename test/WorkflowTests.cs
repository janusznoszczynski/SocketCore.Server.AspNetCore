using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Xunit;
using Newtonsoft.Json;
using SocketCore.Server.AspNetCore;
using System.Linq;
using System.Net.WebSockets;
using SocketCore.Server.AspNetCore.Workflows;
using Newtonsoft.Json.Linq;

namespace SocketCore.Server.AspNetCore.Tests
{
    public class WorkflowTests
    {
        private readonly TestServer _server = null;
        private readonly WebSocketClient _client = null;

        public WorkflowTests()
        {
            _server = new TestServer(new WebHostBuilder().UseStartup<Startup>());
            _client = _server.CreateWebSocketClient();
        }

        [Fact]
        public Task Simple1()
        {
            return Recieve("/workflows");
        }

        private async Task<string> Recieve(string path)
        {
            var ws = await _client.ConnectAsync(new Uri(_server.BaseAddress, path), CancellationToken.None);

            var cmd = await ws.RecieveCommandAsync();
            Assert.Equal("SetConnectionId", cmd.Type);

            var connectionId = cmd.Data.ToString();

            var message = new Message("SimpleNamespace", "SimpleType")
            {
                ConnectionId = connectionId
            };

            await ws.SendDataAsync(new
            {
                Channel = "simple",
                Message = message
            });

            var data = await ws.RecieveDataAsync();
            var reply = (data as JObject).ToObject<Message>();
            Assert.Equal(connectionId, reply.ReplyToMessageId);

            return connectionId;
        }
    }
}

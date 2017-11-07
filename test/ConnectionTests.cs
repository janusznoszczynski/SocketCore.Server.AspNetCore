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

namespace SocketCore.Server.AspNetCore.Tests
{
    public class SimpleConnectionTests
    {
        private readonly TestServer _server = null;
        private readonly WebSocketClient _client = null;

        public SimpleConnectionTests()
        {
            _server = new TestServer(new WebHostBuilder().UseStartup<Startup>());
            _client = _server.CreateWebSocketClient();
        }

        [Fact]
        public Task Simple1()
        {
            return Recieve("/simple");
        }

        [Fact]
        public async Task Simple2()
        {
            var count = 500;

            var tasks = Enumerable.Range(1, count).Select(i => Recieve("/simple"));
            await Task.WhenAll(tasks);

            var connectionIds = tasks.Select(a => a.Result).Distinct().ToArray();
            Assert.Equal(count, connectionIds.Length);
        }

        [Fact]
        public async Task LongRunning1()
        {
            var count = 1;

            var tasks = Enumerable.Range(1, count).Select(i => Recieve("/longrunning"));
            await Task.WhenAll(tasks);

            var connectionIds = tasks.Select(t => t.Result).Distinct().ToArray();
            Assert.Equal(count, connectionIds.Length);
        }

        private async Task<string> Recieve(string path)
        {
            var ws = await _client.ConnectAsync(new Uri(_server.BaseAddress, path), CancellationToken.None);

            var cmd = await ws.RecieveCommandAsync();
            Assert.Equal("SetConnectionId", cmd.Type);

            var connectionId = cmd.Data.ToString();
            await ws.SendDataAsync(connectionId);

            var data = await ws.RecieveDataAsync();
            Assert.Equal($"Reply to: {connectionId}", data);

            return connectionId;
        }
    }
}

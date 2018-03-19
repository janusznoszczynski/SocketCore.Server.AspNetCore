using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using SocketCore.Server.AspNetCore;
using Xunit;

namespace SocketCore.Server.AspNetCore.Tests
{
    public class ConnectionTests
    {
        [Theory]
        [MemberData(nameof(GetServices))]
        public async Task Simple1(IConnectionManager connectionManager)
        {
            using(var server = BuildTestServer(connectionManager))
            {
                await Recieve("/simple", server);
            }
        }

        [Theory]
        [MemberData(nameof(GetServices))]
        public async Task Simple2(IConnectionManager connectionManager)
        {
            var count = 1;

            using(var server = BuildTestServer(connectionManager))
            {
                var tasks = Enumerable.Range(1, count).Select(i => Recieve("/simple", server)).ToArray();
                await Task.WhenAll(tasks);

                var connectionIds = tasks.Select(a => a.Result).Distinct().ToArray();
                Assert.Equal(count, connectionIds.Length);
            }
        }

        [Theory]
        [MemberData(nameof(GetServices))]
        public async Task LongRunning1(IConnectionManager connectionManager)
        {
            var count = 100;

            using(var server = BuildTestServer(connectionManager))
            {
                var tasks = Enumerable.Range(1, count).Select(i => Recieve("/longrunning", server)).ToArray();
                await Task.WhenAll(tasks);

                var connectionIds = tasks.Select(t => t.Result).Distinct().ToArray();
                Assert.Equal(count, connectionIds.Length);
            }
        }

        public static IEnumerable<object[]> GetServices()
        {
            yield return new object[] { new InProcConnectionManager() };
            yield return new object[] { new RedisConnectionManager("localhost", prefix: "ConnectionTests$") };
        }

        private TestServer BuildTestServer<TConnectionManager>(TConnectionManager connectionManager, int port = 80)
        where TConnectionManager : class, IConnectionManager
        {
            var server = new TestServer(new WebHostBuilder()
                .ConfigureServices(services =>
                {
                    services.AddSingleton<IConnectionManager, TConnectionManager>(provider => connectionManager);
                })
                .Configure(app =>
                {
                    app.UseSocketCore("/simple", new SimpleConnection());
                    app.UseSocketCore("/longrunning", new LongRunningConnection());
                }));

            server.BaseAddress = new Uri($"http://localhost:{port}/");
            return server;
        }

        private async Task<string> Recieve(string path, TestServer server)
        {
            var _client = server.CreateWebSocketClient();

            using(var ws = await _client.ConnectAsync(new Uri(server.BaseAddress, path), CancellationToken.None))
            {
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
}
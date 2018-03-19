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
using Newtonsoft.Json.Linq;
using SocketCore.Server.AspNetCore;
using SocketCore.Server.AspNetCore.Workflows;
using Xunit;

namespace SocketCore.Server.AspNetCore.Tests
{
    public class WorkflowTests
    {
        [Theory]
        [MemberData(nameof(GetServices))]
        public async Task Simple1(IConnectionManager connectionManager, IWorkflowManager workflowManager)
        {
            using(var server = BuildTestServer(connectionManager, workflowManager))
            {
                await Recieve("/workflows", "simple", server);
            }
        }

        [Theory]
        [MemberData(nameof(GetServices))]
        public async Task LongRunning1(IConnectionManager connectionManager, IWorkflowManager workflowManager)
        {
            var count = 1;

            using(var server = BuildTestServer(connectionManager, workflowManager))
            {
                var tasks = Enumerable.Range(1, count).Select(i => Recieve("/workflows", "longrunning", server)).ToArray();
                await Task.WhenAll(tasks);

                var connectionIds = tasks.Select(t => t.Result).Distinct().ToArray();
                Assert.Equal(count, connectionIds.Length);
            }
        }

        public static IEnumerable<object[]> GetServices()
        {
            yield return new object[]
            {
                new InProcConnectionManager(),
                    new InProcWorkflowManager()
            };

            yield return new object[]
            {
                new RedisConnectionManager("localhost", prefix: "WorkflowTests$"),
                    new RedisWorkflowManager("localhost", prefix: "WorkflowTests$")
            };
        }

        private TestServer BuildTestServer<TConnectionManager, TWorkflowManager>(
            TConnectionManager connectionManager, TWorkflowManager workflowManager)
        where TConnectionManager : class, IConnectionManager
        where TWorkflowManager : class, IWorkflowManager
        {
            return new TestServer(new WebHostBuilder()
                .ConfigureServices(services =>
                {
                    services.AddSingleton<IConnectionManager, TConnectionManager>(provider => connectionManager);
                    services.AddSingleton<IWorkflowManager, TWorkflowManager>(provider => workflowManager);
                })
                .Configure(app =>
                {
                    app.UseSocketCore("/simple", new SimpleConnection());
                    app.UseSocketCore("/longrunning", new LongRunningConnection());

                    app.UseSocketCoreWorkflows("/workflows");
                }));
        }

        private async Task<string> Recieve(string path, string channel, TestServer server)
        {
            var client = server.CreateWebSocketClient();

            using(var ws = await client.ConnectAsync(new Uri(server.BaseAddress, path), CancellationToken.None))
            {
                var cmd = await ws.RecieveCommandAsync();
                Assert.Equal("SetConnectionId", cmd.Type);

                var connectionId = cmd.Data.ToString();

                var message = new Message("SimpleNamespace", "SimpleType")
                {
                    ConnectionId = connectionId
                };

                await ws.SendDataAsync(new
                {
                    Channel = channel,
                        Message = message
                });

                var data = await ws.RecieveDataAsync();
                var reply = (data as JObject).ToObject<Message>();
                Assert.Equal(connectionId, reply.ReplyToMessageId);

                return connectionId;
            }
        }
    }
}
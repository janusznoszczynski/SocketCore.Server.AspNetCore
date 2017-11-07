using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Xunit;
using Newtonsoft.Json;
using SocketCore.Server.AspNetCore;
using StackExchange.Redis;
using System.Linq;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.Json;
using System.IO;
using System.Text;

namespace SocketCore.Server.AspNetCore.Tests
{
    public class RedisConnectionManagerTests
    {
        private static Random _Random = new Random((int)DateTime.Now.Ticks);
        private readonly IConfigurationRoot _Config = null;
        private readonly ConnectionMultiplexer _Conn = null;

        public RedisConnectionManagerTests()
        {
            _Config = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json")
                .Build();

            var connStr = _Config["RedisConnectionString"];
            _Conn = ConnectionMultiplexer.Connect(connStr);
        }

        [Fact]
        public Task SingleConnection()
        {
            return UsePrefix(async prefix =>
            {
                var connectionId = Guid.NewGuid().ToString();
                var connMgr = new RedisConnectionManager(_Config["RedisConnectionString"], prefix);
                await connMgr.ConnectAsync(connectionId, null);

                var channels = await _Conn.CHANNELS($"{prefix}*");
                Assert.Equal(2, channels.Length);
                Assert.True(channels.Contains($"{prefix}all"));
                Assert.True(channels.Contains($"{prefix}c_{connectionId}"));

                var keys = await _Conn.KEYS($"{prefix}*");
                Assert.Equal(1, keys.Length);
                Assert.True(keys.Contains($"{prefix}n_{connMgr.NodeId}"));
            });
        }

        [Fact]
        public Task MultipleConnections()
        {
            return UsePrefix(async prefix =>
            {
                var connMgr = new RedisConnectionManager(_Config["RedisConnectionString"], prefix);

                var connectionsId = Enumerable.Range(1, 5).Select(a => Guid.NewGuid().ToString()).ToArray();
                Task.WaitAll(connectionsId.Select(connectionId => connMgr.ConnectAsync(connectionId, null)).ToArray());

                var channels = await _Conn.CHANNELS($"{prefix}*");
                Assert.Equal(connectionsId.Length + 1, channels.Length);

                Assert.True(channels.Contains($"{prefix}all"));

                foreach (var connectionId in connectionsId)
                {
                    Assert.True(channels.Contains($"{prefix}c_{connectionId}"));
                }
            });
        }

        [Fact]
        public Task Group1()
        {
            return UsePrefix(async prefix =>
            {
                var connectionId = Guid.NewGuid().ToString();
                var connMgr = new RedisConnectionManager(_Config["RedisConnectionString"], prefix);

                await connMgr.ConnectAsync(connectionId, null);
                await connMgr.AddToGroupAsync(connectionId, "group1");

                var groupKey = $"{prefix}g_group1";
                Assert.True(await _Conn.EXISTS(groupKey));

                var connections = await _Conn.SMEMBERS(groupKey);
                Assert.Equal(1, connections.Length);
                Assert.True(connections.Contains(connectionId));
            });
        }

        [Fact]
        public Task Group2()
        {
            return UsePrefix(async prefix =>
            {
                var connectionId1 = Guid.NewGuid().ToString();
                var connectionId2 = Guid.NewGuid().ToString();
                var connMgr = new RedisConnectionManager(_Config["RedisConnectionString"], prefix);

                await connMgr.ConnectAsync(connectionId1, null);
                await connMgr.AddToGroupAsync(connectionId1, "group1");

                await connMgr.ConnectAsync(connectionId2, null);
                await connMgr.AddToGroupAsync(connectionId2, "group1");

                var groupKey = $"{prefix}g_group1";
                Assert.True(await _Conn.EXISTS(groupKey));

                var connections = await _Conn.SMEMBERS(groupKey);
                Assert.Equal(2, connections.Length);
                Assert.True(connections.Contains(connectionId1));
                Assert.True(connections.Contains(connectionId2));
            });
        }

        [Fact]
        public Task SendToGroup1()
        {
            return UsePrefix(async prefix =>
            {
                var connectionId1 = Guid.NewGuid().ToString();
                var connectionId2 = Guid.NewGuid().ToString();
                var connMgr = new RedisConnectionManager(_Config["RedisConnectionString"], prefix);

                var tcs1 = new TaskCompletionSource<object>();
                var tcs2 = new TaskCompletionSource<object>();

                await connMgr.ConnectAsync(connectionId1, o =>
                {
                    tcs1.SetResult(o);
                    return Task.FromResult(0);
                });

                await connMgr.ConnectAsync(connectionId2, o =>
                {
                    tcs2.SetResult(o);
                    return Task.FromResult(0);
                });

                await connMgr.AddToGroupAsync(connectionId1, "group1");
                await connMgr.AddToGroupAsync(connectionId2, "group1");
                await connMgr.SendToGroupsAsync("Hello", "group1");

                Task.WaitAll(tcs1.Task, tcs2.Task);

                Assert.Equal("Hello", tcs1.Task.Result);
                Assert.Equal("Hello", tcs2.Task.Result);
            });
        }

        [Fact]
        public Task Disconnect1()
        {
            return UsePrefix(async prefix =>
            {
                var connectionId = Guid.NewGuid().ToString();
                var connMgr = new RedisConnectionManager(_Config["RedisConnectionString"], prefix);
                var handle = await connMgr.ConnectAsync(connectionId, o => null);

                var channels = await _Conn.CHANNELS($"{prefix}*");
                Assert.Equal(2, channels.Length);

                Assert.True(channels.Contains($"{prefix}all"));
                Assert.True(channels.Contains($"{prefix}c_{connectionId}"));

                var connections = await connMgr.GetConnectionsAsync();

                await connMgr.DisconnectAsync(connectionId, handle);
            });
        }

        [Fact]
        public Task Test()
        {
            return UsePrefix(async prefix =>
            {
                var connectionId = Guid.NewGuid().ToString();
                var connMgr = new RedisConnectionManager(_Config["RedisConnectionString"], prefix);
                var handle = await connMgr.ConnectAsync(connectionId, o => null);
                await connMgr.AddToGroupAsync(connectionId, "group1");
                await connMgr.DisconnectAsync(connectionId, handle);
            });
        }

        private string RandomPrefix(int size)
        {
            var builder = new StringBuilder();

            for (int i = 0; i < size; i++)
            {
                builder.Append(Convert.ToChar(Convert.ToInt32(Math.Floor(26 * _Random.NextDouble() + 65))));
            }

            return builder.ToString() + "_";
        }

        private async Task Clenup(string prefix)
        {
            await Task.WhenAll((await _Conn.KEYS($"{prefix}*")).Select(k => _Conn.DEL(k)));
        }

        private async Task UsePrefix(Func<string, Task> func)
        {
            var prefix = RandomPrefix(5);

            try
            {
                await func(prefix);
            }
            finally
            {
                await Clenup(prefix);
            }
        }
    }
}
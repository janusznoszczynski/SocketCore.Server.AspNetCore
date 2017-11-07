using System;
using System.Threading.Tasks;
using System.Linq;
using StackExchange.Redis;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.IO;

namespace SocketCore.Server.AspNetCore
{
    public class RedisConnectionManager : IConnectionManager, IDisposable
    {
        private Task _DrumBeatTask = null;
        private readonly string _NodeId = Guid.NewGuid().ToString();
        private readonly string _Prefix = "$";
        private ConnectionMultiplexer _Instance = null;


        public RedisConnectionManager(string configuration, TextWriter log = null, string prefix = "$")
        {
            _Instance = ConnectionMultiplexer.Connect(configuration, log);
            _Prefix = prefix;
        }

        public RedisConnectionManager(ConfigurationOptions configuration, TextWriter log = null, string prefix = "$")
        {
            _Instance = ConnectionMultiplexer.Connect(configuration, log);
            _Prefix = prefix;
        }

        public RedisConnectionManager(string configuration, string prefix)
            : this(configuration, null, prefix)
        {
        }

        public RedisConnectionManager(TextWriter log = null)
            : this("localhost", log)
        {
        }

        ~RedisConnectionManager()
        {
            //free only unmanaged resources
            Dispose(false);
        }

        public void Dispose()
        {
            //free managed and unmanaged resources
            Dispose(true);

            //remove this object from finalization queue because it is already cleaned up
            GC.SuppressFinalize(this);
        }

        public string NodeId
        {
            get
            {
                return _NodeId;
            }
        }

        private void Dispose(bool disposing)
        {
            //free unmanaged resources

            if (disposing)
            {
                //free managed resources

                if (_Instance != null)
                {
                    _Instance.Dispose();
                    _Instance = null;
                }

                if (_DrumBeatTask != null)
                {
                    _DrumBeatTask.Dispose();
                    _DrumBeatTask = null;
                }
            }
        }


        public async Task<object> ConnectAsync(string connectionId, Func<object, Task> callback)
        {
            var handler = new Action<RedisChannel, RedisValue>(async (chn, msg) =>
            {
                var message = JsonConvert.DeserializeObject<object>(msg);
                await callback(message);
            });

            await Task.WhenAll(
                RedisSubscribeAsync($"{_Prefix}all", handler),
                RedisSubscribeAsync($"{_Prefix}c_{connectionId}", handler));

            await EnsureDrumBeatStarted(connectionId);
            await _Instance.SADD($"{_Prefix}n_{_NodeId}", connectionId);

            return handler;
        }

        public Task DisconnectAsync(string connectionId, object handle)
        {
            return Task.WhenAll(
                _Instance.SREM($"{_Prefix}n_{_NodeId}", connectionId),
                RedisSetRemoveMatchAsync($"{_Prefix}g_*", connectionId),
                RedisUnsubscribeAsync($"{_Prefix}c_{connectionId}", handle as Action<RedisChannel, RedisValue>),
                RedisUnsubscribeAsync($"{_Prefix}all", handle as Action<RedisChannel, RedisValue>));
        }

        public Task AddToGroupAsync(string connectionId, string group)
        {
            return _Instance.SADD($"{_Prefix}g_{group}", connectionId);
        }

        public Task RemoveFromGroupAsync(string connectionId, string group)
        {
            return _Instance.SREM($"{_Prefix}g_{group}", connectionId);
        }

        public async Task<IEnumerable<string>> GetConnectionsAsync()
        {
            var channels = await _Instance.CHANNELS($"{_Prefix}c_*");
            return channels.Select(a => a.Replace($"{_Prefix}c_", string.Empty)).ToArray();
        }

        public async Task<IEnumerable<string>> GetGroupsAsync()
        {
            var groups = await _Instance.KEYS($"{_Prefix}g_*");
            return groups.Select(a => a.Replace($"{_Prefix}g_", string.Empty)).ToArray();
        }

        public Task SendToConnectionsAsync(object data, params string[] connectionIds)
        {
            return Task.WhenAll(connectionIds.Select(connectionId => RedisPublishAsync($"{_Prefix}c_{connectionId}", data)));
        }

        public Task SendToGroupsAsync(object data, params string[] groups)
        {
            return Task.WhenAll(groups.Select(group => RedisSetPublishAsync($"{_Prefix}g_{group}", data)));
        }

        public Task SendToAllAsync(object data)
        {
            return RedisPublishAsync($"{_Prefix}all", data);
        }

        public async Task CleanupEmptyGroups()
        {
            //create temp set of all connections id
            var nodes = await _Instance.KEYS($"{_Prefix}n_*");
            var tmpKey = $"{_Prefix}{Guid.NewGuid().ToString()}";
            await _Instance.SUNIONSTORE(tmpKey, nodes);

            //remove from groups not existing connections id
            var groups = await _Instance.KEYS($"{_Prefix}g_*");
            await Task.WhenAll(groups.Select(g => _Instance.SUNIONSTORE(g, g, tmpKey)));

            //remove temp set
            await _Instance.DEL(tmpKey);
        }


        private async Task EnsureDrumBeatStarted(string connectionId)
        {
            if (!await _Instance.GetDatabase().KeyExistsAsync($"{_Prefix}n_{_NodeId}"))
            {
                _DrumBeatTask = Task.Factory.StartNew(async () =>
                {
                    while (true)
                    {
                        await _Instance.GetDatabase().KeyExpireAsync($"{_Prefix}n_{_NodeId}", TimeSpan.FromSeconds(10));
                        await Task.Delay(5000);
                    }
                });
            }
        }

        private Task RedisSetPublishAsync(string setName, object messages)
        {
            var script = $@"
                local channels = redis.call('SMEMBERS', KEYS[1])
                local res = {{}}
                for i = 1, #channels do
                    table.insert(res, redis.call('PUBLISH', '{_Prefix}c_' .. channels[i], ARGV[1]))
                end
                return res";

            RedisValue[] values = { JsonConvert.SerializeObject(messages) };
            return _Instance.GetDatabase().ScriptEvaluateAsync(script, new RedisKey[] { setName }, values);
        }

        private Task RedisPublishAsync(string channel, object messages)
        {
            return _Instance.GetSubscriber().PublishAsync(channel, JsonConvert.SerializeObject(messages));
        }

        private Task RedisSubscribeAsync(string channel, Action<RedisChannel, RedisValue> handler)
        {
            return _Instance.GetSubscriber().SubscribeAsync(channel, handler);
        }

        private Task RedisUnsubscribeAsync(string channel, Action<RedisChannel, RedisValue> handler)
        {
            return _Instance.GetSubscriber().UnsubscribeAsync(channel, handler);
        }

        private Task RedisSetRemoveMatchAsync(string pattern, RedisValue value)
        {
            var script = @"
                local keys = redis.call('KEYS', '" + pattern + @"')
                local res = {}
                for i = 1, #keys do
                    table.insert(res, redis.call('SREM', keys[i], ARGV[1]))
                end
                return res";

            return _Instance.GetDatabase().ScriptEvaluateAsync(script, new RedisKey[] { pattern }, new RedisValue[] { value });
        }
    }
}
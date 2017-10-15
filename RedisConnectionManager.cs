using System;
using System.Threading.Tasks;
using System.Linq;
using StackExchange.Redis;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.IO;

namespace SocketCore.Server.AspNetCore
{
    public class RedisConnectionManager : IConnectionManager
    {
        private ConnectionMultiplexer _Instance = null;


        public RedisConnectionManager(string configuration, TextWriter log = null)
        {
            _Instance = ConnectionMultiplexer.Connect(configuration, log);
        }

        public RedisConnectionManager(ConfigurationOptions configuration, TextWriter log = null)
        {
            _Instance = ConnectionMultiplexer.Connect(configuration, log);
        }

        public RedisConnectionManager(TextWriter log = null)
            : this("localhost", log)
        {
        }


        public async Task<object> ConnectAsync(string connectionId, Func<object, Task> callback)
        {
            var handler = new Action<RedisChannel, RedisValue>(async (chn, msg) =>
            {
                var message = JsonConvert.DeserializeObject<object>(msg);
                await callback(message);
            });

            await Task.WhenAll(
                RedisSubscribeAsync("$all", handler),
                RedisSubscribeAsync($"$c_{connectionId}", handler));

            return handler;
        }

        public Task DisconnectAsync(string connectionId, object handle)
        {
            // ///TODO Lua script
            // var groups = await RedisSetMembersAsync($"$cg_{connectionId}");
            // var tasks = groups.Select(a => RedisSetRemoveAsync($"$g_{(string)a}", connectionId));
            // await Task.WhenAll(tasks);
            // //--------------------------------

            return Task.WhenAll(
                RedisSetRemoveMatchAsync("$g_*", connectionId),
                // RedisKeyDeleteAsync($"$cg_{connectionId}"),
                RedisUnsubscribeAsync($"$c_{connectionId}", handle as Action<RedisChannel, RedisValue>),
                RedisUnsubscribeAsync("$all", handle as Action<RedisChannel, RedisValue>));
        }

        // public async Task KeyExpireAsync(string connectionId, object handle)
        // {
        //     _Instance.GetDatabase().KeyExpireAsync();
        // }

        public Task AddToGroupAsync(string connectionId, string group)
        {
            return Task.WhenAll(
                RedisSetAddAsync($"$g_{group}", connectionId));
                //RedisSetAddAsync($"$cg_{connectionId}", group));
        }

        public Task RemoveFromGroupAsync(string connectionId, string group)
        {
            return Task.WhenAll(
                RedisSetRemoveAsync($"$g_{group}", connectionId));
                //RedisSetRemoveAsync($"$cg_{connectionId}", group));
        }

        public async Task<IEnumerable<string>> GetConnectionsAsync()
        {
            var result = await RedisSetMembersAsync("$allconnectionsset");
            return result.Select(a => (string)a);
        }


        public Task SendToConnectionsAsync(object data, params string[] connectionIds)
        {
            return Task.WhenAll(connectionIds.Select(connectionId => RedisPublishAsync($"$c_{connectionId}", data)));
        }

        public Task SendToGroupsAsync(object data, params string[] groups)
        {
            return Task.WhenAll(groups.Select(group => RedisSetPublishAsync($"$g_{group}", data)));
        }

        public Task SendToAllAsync(object data)
        {
            return RedisPublishAsync("$all", data);
        }



        private Task RedisSetPublishAsync(string setName, object messages)
        {
            var script = @"
                local channels = redis.call('SMEMBERS', KEYS[1])
                local res = {}
                for i = 1, #channels do
                    table.insert(res, redis.call('PUBLISH', '$c_' .. channels[i], ARGV[1]))
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

        private Task RedisKeyDeleteAsync(RedisKey key, CommandFlags flags = CommandFlags.None)
        {
            return _Instance.GetDatabase().KeyDeleteAsync(key, flags);
        }

        private Task RedisSetAddAsync(RedisKey key, RedisValue value, CommandFlags flags = CommandFlags.None)
        {
            return _Instance.GetDatabase().SetAddAsync(key, value, flags);
        }

        private Task RedisSetRemoveAsync(RedisKey key, RedisValue value, CommandFlags flags = CommandFlags.None)
        {
            return _Instance.GetDatabase().SetRemoveAsync(key, value, flags);
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

        private Task<RedisValue[]> RedisSetMembersAsync(RedisKey key, CommandFlags flags = CommandFlags.None)
        {
            return _Instance.GetDatabase().SetMembersAsync(key, flags);
        }
    }
}
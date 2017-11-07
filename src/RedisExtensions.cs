using System;
using System.Threading.Tasks;
using System.Linq;
using StackExchange.Redis;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.IO;

namespace SocketCore.Server.AspNetCore
{
    public static class RedisExtensions
    {
        public static Task<long> DEL(this ConnectionMultiplexer conn, params string[] keys)
        {
            return conn.GetDatabase().KeyDeleteAsync(keys.Select(k => (RedisKey)k).ToArray());
        }

        public static Task<bool> EXISTS(this ConnectionMultiplexer conn, string key)
        {
            return conn.GetDatabase().KeyExistsAsync(key);
        }

        public static async Task<string[]> KEYS(this ConnectionMultiplexer conn, string pattern)
        {
            var result = (string[])await conn.GetDatabase().ExecuteAsync("KEYS", pattern);
            return result;
        }


        public static Task<bool> SADD(this ConnectionMultiplexer conn, string key, string value)
        {
            return conn.GetDatabase().SetAddAsync(key, value);
        }

        public static Task<bool> SREM(this ConnectionMultiplexer conn, string key, string value)
        {
            return conn.GetDatabase().SetRemoveAsync(key, value);
        }

        public static Task<RedisValue[]> SMEMBERS(this ConnectionMultiplexer conn, string key)
        {
            return conn.GetDatabase().SetMembersAsync(key);
        }

        public static async Task<int> SUNIONSTORE(this ConnectionMultiplexer conn, string keyDest, params string[] keys)
        {
            var args = new List<object>();
            args.Add(keyDest);
            args.AddRange(keys);

            var result = (RedisResult)await conn.GetDatabase().ExecuteAsync("SUNIONSTORE", args.ToArray());
            return (int)result;
        }


        public static async Task<string[]> CHANNELS(this ConnectionMultiplexer conn, string pattern)
        {
            var result = (string[])await conn.GetDatabase().ExecuteAsync("PUBSUB", "CHANNELS", pattern);
            return result;
        }

        public static async Task<int> NUMSUB(this ConnectionMultiplexer conn, string pattern)
        {
            var result = (RedisValue[])await conn.GetDatabase().ExecuteAsync("PUBSUB", "NUMSUB", pattern);
            return (int)result[1];
        }

        public static async Task<int> NUMPAT(this ConnectionMultiplexer conn, string pattern)
        {
            var result = (RedisValue[])await conn.GetDatabase().ExecuteAsync("PUBSUB", "NUMPAT", pattern);
            return (int)result[1];
        }
    }
}
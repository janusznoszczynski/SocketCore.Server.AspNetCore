using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using System.Linq;
using System.Collections.Generic;

namespace SocketCore.Server.AspNetCore
{
    public class InProcConnectionManager : IConnectionManager
    {
        private static ConcurrentDictionary<string, Func<object, Task>> _Connections = new ConcurrentDictionary<string, Func<object, Task>>();
        private static ConcurrentDictionary<string, ConcurrentBag<Func<object, Task>>> _Groups = new ConcurrentDictionary<string, ConcurrentBag<Func<object, Task>>>();

        public Task<object> ConnectAsync(string connectionId, Func<object, Task> callback)
        {
            _Connections.TryAdd(connectionId, callback);
            return Task.FromResult((object)null);
        }

        public Task DisconnectAsync(string connectionId, object handle)
        {
            Func<object, Task> callback;
            _Connections.TryRemove(connectionId, out callback);
            return Task.FromResult(0);
        }

        public Task AddToGroupAsync(string connectionId, string group)
        {
            Func<object, Task> handler = null;

            if (_Connections.TryGetValue(connectionId, out handler))
            {
                ConcurrentBag<Func<object, Task>> handlers = null;

                if (_Groups.TryGetValue(group, out handlers))
                {
                    handlers.Add(handler);
                }
                else
                {
                    handlers = new ConcurrentBag<Func<object, Task>>();
                }
            }
            else
            {
                throw new InvalidOperationException($"Connection '{connectionId}' not found");
            }

            return Task.FromResult(0);
        }

        public Task RemoveFromGroupAsync(string connectionId, string group)
        {
            Func<object, Task> handler = null;

            if (_Connections.TryGetValue(connectionId, out handler))
            {
                ConcurrentBag<Func<object, Task>> handlers = null;

                if (_Groups.TryGetValue(group, out handlers))
                {
                    handlers.TryTake(out handler);
                }
            }
            else
            {
                throw new InvalidOperationException($"Connection '{connectionId}' not found");
            }

            return Task.FromResult(0);
        }

        public Task<IEnumerable<string>> GetConnectionsAsync()
        {
            return Task.FromResult(_Connections.Keys.AsEnumerable());
        }


        public Task SendToConnectionsAsync(object data, params string[] connectionIds)
        {
            var tasks = new List<Task>();

            foreach (var connectionId in connectionIds)
            {
                Func<object, Task> callback;

                if (_Connections.TryGetValue(connectionId, out callback))
                {
                    tasks.Add(callback(data));
                }
                else
                {
                    throw new InvalidOperationException($"Connection '{connectionId}' not found");
                }
            }

            return Task.WhenAll(tasks);
        }

        public Task SendToGroupsAsync(object data, params string[] groups)
        {
            var tasks = new List<Task>();

            foreach (var group in groups)
            {
                ConcurrentBag<Func<object, Task>> handlers;

                if (_Groups.TryGetValue(group, out handlers))
                {
                    tasks.AddRange(handlers.Select(c => c(data)));
                }
                else
                {
                    throw new InvalidOperationException($"Group '{group}' not found");
                }
            }

            return Task.WhenAll(tasks);
        }

        public Task SendToAllAsync(object data)
        {
            var tasks = _Connections.Values.Select(callback => callback(data)).ToArray();
            return Task.WhenAll(tasks);
        }
    }
}
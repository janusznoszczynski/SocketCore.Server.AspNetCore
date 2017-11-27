using System.Collections.Generic;
using System.Threading.Tasks;

namespace SocketCore.Server.AspNetCore
{
    public class Connection
    {
        private IConnectionManager _connectionManager = null;


        internal Task Connected(string connectionId)
        {
            return OnConnected(connectionId);
        }

        internal Task Recieved(string connectionId, object data)
        {
            return OnReceived(connectionId, data);
        }


        internal void SetConnectionManager(IConnectionManager connectionManager)
        {
            _connectionManager = connectionManager;
        }


        protected internal Task SendToConnectionsAsync(object data, params string[] connectionIds)
        {
            return _connectionManager.SendToConnectionsAsync(data, connectionIds);
        }

        protected internal Task SendToGroupsAsync(object data, params string[] groups)
        {
            return _connectionManager.SendToGroupsAsync(data, groups);
        }

        protected internal Task SendToAllAsync(object data)
        {
            return _connectionManager.SendToAllAsync(data);
        }

        protected internal Task AddToGroupAsync(string connectionId, string group)
        {
            return _connectionManager.AddToGroupAsync(connectionId, group);
        }

        protected internal Task RemoveFromGroupAsync(string connectionId, string group)
        {
            return _connectionManager.RemoveFromGroupAsync(connectionId, group);
        }

        protected internal Task<IEnumerable<string>> GetConnectionsAsync()
        {
            return _connectionManager.GetConnectionsAsync();
        }


        protected virtual Task OnConnected(string connectionId)
        {
            return Task.FromResult(0);
        }

        protected virtual Task OnDisconnected(string connectionId)
        {
            return Task.FromResult(0);
        }

        protected virtual Task OnReceived(string connectionId, object data)
        {
            return Task.FromResult(0);
        }

        protected virtual Task OnReconnected(string connectionId)
        {
            return Task.FromResult(0);
        }
    }
}
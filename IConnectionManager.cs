using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SocketCore.Server.AspNetCore
{
    public interface IConnectionManager
    {
        Task<object> ConnectAsync(string connectionId, Func<object, Task> callback);
        Task DisconnectAsync(string connectionId, object handle);

        Task SendToConnectionsAsync(object data, params string[] connectionIds);
        Task SendToGroupsAsync(object data, params string[] groups);
        Task SendToAllAsync(object data);

        Task AddToGroupAsync(string connectionId, string group);
        Task RemoveFromGroupAsync(string connectionId, string group);
        
        Task<IEnumerable<string>> GetConnectionsAsync();
    }
}
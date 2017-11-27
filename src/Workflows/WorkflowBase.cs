using System;
using System.Threading.Tasks;
using SocketCore.Server.AspNetCore;
using System.Linq;
using System.Collections.Generic;

namespace SocketCore.Server.AspNetCore.Workflows
{
    public abstract class WorkflowBase
    {
        private WorkflowConnection _Connection = null;
        private string _SenderId = "";

        internal protected abstract Task ExecuteAsync(Message message);

        internal void SetConnection(WorkflowConnection connection)
        {
            _Connection = connection;
        }

        internal void SetSenderId(string senderId)
        {
            _SenderId = senderId;
        }

        protected Task ReplyAsync(Message message, params Message[] replies)
        {
            message.Reply(replies);

            foreach (var reply in replies)
            {
                reply.SenderId = _SenderId;
            }

            if (1 == replies.Count())
            {
                return _Connection.SendToConnectionsAsync(replies.First(), message.ConnectionId);
            }
            else
            {
                return _Connection.SendToConnectionsAsync(replies, message.ConnectionId);
            }
        }

        protected internal Task SendToConnectionsAsync(Message message, params string[] connectionsIds)
        {
            return _Connection.SendToConnectionsAsync(message, connectionsIds);
        }

        protected internal Task SendToGroupsAsync(Message message, params string[] groups)
        {
            return _Connection.SendToGroupsAsync(message, groups);
        }

        protected internal Task SendToAllAsync(Message message)
        {
            return _Connection.SendToAllAsync(message);
        }

        protected internal Task AddToGroupAsync(string connectionId, string group)
        {
            return _Connection.AddToGroupAsync(connectionId, group);
        }

        protected internal Task RemoveFromGroupAsync(string connectionId, string group)
        {
            return _Connection.RemoveFromGroupAsync(connectionId, group);
        }

        protected internal Task<IEnumerable<string>> GetConnectionsAsync()
        {
            return _Connection.GetConnectionsAsync();
        }
    }
}
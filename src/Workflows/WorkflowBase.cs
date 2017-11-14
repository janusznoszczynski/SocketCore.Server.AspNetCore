using System;
using System.Threading.Tasks;
using SocketCore.Server.AspNetCore;
using System.Linq;


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

        protected Task Reply(Message message, params Message[] replies)
        {
            foreach (var reply in replies)
            {
                reply.MessageId = Guid.NewGuid().ToString();
                reply.SessionId = message.SessionId;
                reply.SenderId = _SenderId;
                reply.ReplyToMessageId = message.ConnectionId;
            }

            return _Connection.SendToConnectionsAsync(replies, message.ConnectionId);
        }
    }
}
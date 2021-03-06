using System;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SocketCore.Server.AspNetCore;
using SocketCore.Server.AspNetCore.Workflows.Messages;

namespace SocketCore.Server.AspNetCore.Workflows
{
    public class WorkflowConnection : Connection
    {
        private IWorkflowManager _WorkflowManager = null;

        public WorkflowConnection(IWorkflowManager manager)
        {
            _WorkflowManager = manager;
        }

        internal Task RegisterWorkflow(string channel, WorkflowBase workflow)
        {
            return _WorkflowManager.Register(channel, workflow);
        }

        protected override async Task OnReceived(string connectionId, object data)
        {
            dynamic payload = data;

            var channel = payload.Channel?.ToString();

            var message = (payload.Message as JObject).ToObject<Message>();
            message.ConnectionId = connectionId;

            await _WorkflowManager.Produce(channel, message);
        }

        protected override async Task OnConnected(string connectionId)
        {
            await _WorkflowManager.Produce($"WokrflowEvents", new OnConnectedMessage(connectionId));
        }

        protected override async Task OnDisconnected(string connectionId)
        {
            await _WorkflowManager.Produce($"WokrflowEvents", new OnDisconnectedMessage(connectionId));
        }
    }
}
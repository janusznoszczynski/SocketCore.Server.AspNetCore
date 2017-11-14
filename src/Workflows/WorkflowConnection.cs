using System;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SocketCore.Server.AspNetCore;


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
           return  _WorkflowManager.Register(channel, workflow);
        }

        protected override async Task OnReceived(string connectionId, object data)
        {
            dynamic payload = data;

            var channel = payload.Channel?.ToString();

            var message = (payload.Message as JObject).ToObject<Message>();
            message.ConnectionId = connectionId;

            await _WorkflowManager.Produce(channel, message);
        }
    }
}
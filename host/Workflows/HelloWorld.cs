using System.Threading.Tasks;
using SocketCore.Server.AspNetCore.Workflows;

namespace host.Workflows
{
    [SubscribeChannel("HelloWorld")]
    public class HelloWorld : SafeWorkflowBase
    {
        protected override async Task SafeExecuteAsync(Message message)
        {
            if (message.IsMatch("HelloWorld", "Greeting"))
            {
                await AddToGroupAsync(message.ConnectionId, "all");
                await SendToGroupsAsync(new Message("HelloWorld", "Response", "Hello World from: " + message.ConnectionId), "all");
            }
        }
    }
}
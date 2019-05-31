using System.Threading.Tasks;
using SocketCore.Server.AspNetCore.Workflows;

namespace host.Workflows
{
    [SubscribeChannel("HelloWorld")]
    public class HelloWorld : WorkflowBase
    {
        protected override async Task ExecuteAsync(Message message)
        {
            if (message.IsMatch("HelloWorld", "Greeting"))
            {
                // await AddToGroupAsync(message.ConnectionId, "all");
                await SendToConnectionsAsync(new Message("HelloWorld", "Response", "Hello World from: " + message.Data), message.ConnectionId);
            }
        }
    }
}
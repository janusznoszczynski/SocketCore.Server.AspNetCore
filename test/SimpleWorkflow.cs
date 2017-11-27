using System.Threading.Tasks;
using SocketCore.Server.AspNetCore.Workflows;

namespace SocketCore.Server.AspNetCore.Tests
{
    [SubscribeChannel("simple")]
    [SubscribeWorkflowsEventsChannel]
    public class SimpleWorkflow : WorkflowBase
    {
        protected override async Task ExecuteAsync(Message message)
        {
            if (message.IsMatch("SimpleNamespace", "SimpleType"))
            {
                await ReplyAsync(message, new Message());
            }
            else
            {
            }
        }
    }
}
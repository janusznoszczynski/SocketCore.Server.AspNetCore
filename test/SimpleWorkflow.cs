using System.Threading.Tasks;
using SocketCore.Server.AspNetCore.Workflows;

namespace SocketCore.Server.AspNetCore.Tests
{
    [SubscribeChannel("simple")]
    public class SimpleWorkflow : WorkflowBase
    {
        protected override Task ExecuteAsync(Message message)
        {
            return Reply(message, new Message());
        }
    }
}
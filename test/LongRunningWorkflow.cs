using System;
using System.Threading.Tasks;
using SocketCore.Server.AspNetCore.Workflows;

namespace SocketCore.Server.AspNetCore.Tests
{
    [SubscribeChannel("longrunning")]
    public class LongRunningWorkflow : WorkflowBase
    {
        protected override async Task ExecuteAsync(Message message)
        {
            await Task.Delay(TimeSpan.FromSeconds(10));
            await ReplyAsync(message, new Message());
        }
    }
}
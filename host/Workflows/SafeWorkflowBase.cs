using System;
using System.Threading.Tasks;
using SocketCore.Server.AspNetCore.Workflows;

namespace host.Workflows
{
    public abstract class SafeWorkflowBase : WorkflowBase
    {
        protected abstract Task SafeExecuteAsync(Message message);

        protected override async Task ExecuteAsync(Message message)
        {
            try
            {
                await SafeExecuteAsync(message);
            }
            catch (Exception ex)
            {
                throw;
            }
        }
    }
}
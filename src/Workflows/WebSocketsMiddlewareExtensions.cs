
using System;
using System.Reflection;
using Microsoft.AspNetCore.Builder;
using System.Linq;
using System.Threading.Tasks;

namespace SocketCore.Server.AspNetCore.Workflows
{
    public static class WebSocketsMiddlewareExtensions
    {
        public static IApplicationBuilder UseSocketCoreWorkflows(this IApplicationBuilder builder, string url, string senderId = "")
        {
            builder.UseWebSockets();

            var workflowManager = builder.ApplicationServices.GetService(typeof(IWorkflowManager)) as IWorkflowManager;
            var connection = new Workflows.WorkflowConnection(workflowManager);

            var workflowBaseType = typeof(WorkflowBase);
            var subscribeChannelAttributeType = typeof(SubscribeChannelAttribute);

            var assemblies = AppDomain.CurrentDomain.GetAssemblies();
            var types = assemblies.SelectMany(a => a.GetTypes());
            var workflowTypes = types.Where(t => !t.IsAbstract && workflowBaseType.IsAssignableFrom(t)).ToArray();

            Parallel.ForEach(workflowTypes, async t =>
            {
                var workflow = (WorkflowBase)Activator.CreateInstance(t);
                workflow.SetConnection(connection);
                workflow.SetSenderId(senderId);
                
                var attr = (SubscribeChannelAttribute)Attribute.GetCustomAttribute(t, subscribeChannelAttributeType);

                if (attr != null)
                {
                    foreach (var channel in attr.Channels)
                    {
                       await connection.RegisterWorkflow(channel, workflow);
                    }
                }
            });

            return builder.UseMiddleware<WebSocketsMiddleware>(new WebSocketsMiddlewareOptions()
            {
                Path = url,
                Connection = connection
            });
        }
    }
}
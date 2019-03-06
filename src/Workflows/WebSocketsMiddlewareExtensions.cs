using System;
using System.Reflection;
using Microsoft.AspNetCore.Builder;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;

namespace SocketCore.Server.AspNetCore.Workflows
{
    public static class WebSocketsMiddlewareExtensions
    {
        public static IApplicationBuilder UseSocketCoreWorkflows(this IApplicationBuilder builder, string url, string senderId = "")
        {
            builder.UseWebSockets();

            var workflowManager = builder.ApplicationServices.GetService(typeof(IWorkflowManager)) as IWorkflowManager;
            var connection = new WorkflowConnection(workflowManager);

            var workflowBaseType = typeof(WorkflowBase);
            var subscribeWorkflowsEventsChannelAttributeType = typeof(SubscribeWorkflowsEventsChannelAttribute);
            var subscribeChannelAttributeType = typeof(SubscribeChannelAttribute);

            var assemblies = AppDomain.CurrentDomain.GetAssemblies();
            var types = assemblies.SelectMany(a => a.GetTypes());
            var workflowTypes = types.Where(t => !t.IsAbstract && workflowBaseType.IsAssignableFrom(t)).ToArray();

            Parallel.ForEach(workflowTypes, async t =>
            {
                var workflow = (WorkflowBase)ActivatorUtilities.CreateInstance(builder.ApplicationServices, t);
                workflow.SetConnection(connection);
                workflow.SetSenderId(senderId);
                workflow.SetServicesProvider(builder.ApplicationServices);

                var attr1 = (SubscribeWorkflowsEventsChannelAttribute)Attribute.GetCustomAttribute(t, subscribeWorkflowsEventsChannelAttributeType);

                if (attr1 != null)
                {
                    await connection.RegisterWorkflow($"{workflowManager.Prefix}WokrflowEvents", workflow);
                }

                var attr2 = (SubscribeChannelAttribute)Attribute.GetCustomAttribute(t, subscribeChannelAttributeType);

                if (attr2 != null)
                {
                    foreach (var channel in attr2.Channels)
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
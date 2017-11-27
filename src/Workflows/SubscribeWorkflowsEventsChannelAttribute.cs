using System;

namespace SocketCore.Server.AspNetCore.Workflows
{
    [AttributeUsage(validOn: AttributeTargets.Class)]
    public class SubscribeWorkflowsEventsChannelAttribute : Attribute
    {
    }
}
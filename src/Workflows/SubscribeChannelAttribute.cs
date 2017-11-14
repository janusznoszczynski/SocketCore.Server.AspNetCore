using System;
using System.Threading.Tasks;
using SocketCore.Server.AspNetCore;


namespace SocketCore.Server.AspNetCore.Workflows
{
    [AttributeUsage(validOn: AttributeTargets.Class)]
    public class SubscribeChannelAttribute : Attribute
    {
        public string[] Channels { get; set; }

        public SubscribeChannelAttribute(params string[] channels)
        {
            Channels = channels;
        }
    }
}
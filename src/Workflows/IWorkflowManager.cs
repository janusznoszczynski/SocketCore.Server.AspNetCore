using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using SocketCore.Server.AspNetCore;


namespace SocketCore.Server.AspNetCore.Workflows
{
    public interface IWorkflowManager
    {
        Task Produce(string channel, Message message);
        Task Register(string channel, WorkflowBase workflow);
        string Prefix { get; }
    }
}
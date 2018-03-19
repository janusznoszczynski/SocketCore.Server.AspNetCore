using System;
using System.Threading.Tasks;
using System.Linq;
using StackExchange.Redis;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.IO;
using SocketCore.Server.AspNetCore.Workflows.Messages;
using System.Collections.Concurrent;

namespace SocketCore.Server.AspNetCore.Workflows
{
    public class InProcWorkflowManager : IWorkflowManager
    {
        private readonly string _Prefix = "#";
        private static ConcurrentDictionary<string, ConcurrentBag<WorkflowBase>> _Subscriptions = new ConcurrentDictionary<string, ConcurrentBag<WorkflowBase>>();

        public InProcWorkflowManager(string prefix = "#")
        {
            _Prefix = prefix;
        }

        public async Task Produce(string channel, Message message)
        {
            var channelKey = $"{_Prefix}{channel}";

            if (_Subscriptions.ContainsKey(channelKey))
            {
                // Queue message only if any subscriber is waiting for it
                var workflows = _Subscriptions[channelKey];

                foreach (var workflow in workflows)
                {
                    try
                    {
                        var messageCopy = JsonConvert.DeserializeObject<Message>(JsonConvert.SerializeObject(message));
                        await workflow.ExecuteAsync(message);
                    }
                    catch (Exception ex)
                    {
                        await Produce($"{_Prefix}WokrflowEvents", new OnExceptionMessage(ex));
                    }
                }
            }
        }

        public Task Register(string channel, WorkflowBase workflow)
        {
            var channelKey = $"{_Prefix}{channel}";

            _Subscriptions.AddOrUpdate(channelKey, new ConcurrentBag<WorkflowBase>() { workflow }, (key, oldValue) =>
            {
                oldValue.Add(workflow);
                return oldValue;
            });

            return Task.CompletedTask;
        }

        public string Prefix
        {
            get
            {
                return _Prefix;
            }
        }
    }
}
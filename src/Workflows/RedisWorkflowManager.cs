using System;
using System.Threading.Tasks;
using System.Linq;
using StackExchange.Redis;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.IO;
using SocketCore.Server.AspNetCore.Workflows.Messages;

namespace SocketCore.Server.AspNetCore.Workflows
{
    public class RedisWorkflowManager : IWorkflowManager, IDisposable
    {
        private readonly string _Prefix = "#";
        private ConnectionMultiplexer _Instance = null;


        public RedisWorkflowManager(string configuration, TextWriter log = null, string prefix = "#")
        {
            _Instance = ConnectionMultiplexer.Connect(configuration, log);
            _Prefix = prefix;
        }

        public RedisWorkflowManager(ConfigurationOptions configuration, TextWriter log = null, string prefix = "#")
        {
            _Instance = ConnectionMultiplexer.Connect(configuration, log);
            _Prefix = prefix;
        }

        public RedisWorkflowManager(string configuration, string prefix)
            : this(configuration, null, prefix)
        {
        }

        public RedisWorkflowManager(TextWriter log = null)
            : this("localhost", log)
        {
        }

        ~RedisWorkflowManager()
        {
            //free only unmanaged resources
            Dispose(false);
        }

        public void Dispose()
        {
            //free managed and unmanaged resources
            Dispose(true);

            //remove this object from finalization queue because it is already cleaned up
            GC.SuppressFinalize(this);
        }

        private void Dispose(bool disposing)
        {
            //free unmanaged resources

            if (disposing)
            {
                //free managed resources

                if (_Instance != null)
                {
                    _Instance.Dispose();
                    _Instance = null;
                }
            }
        }

        public async Task Produce(string channel, Message message)
        {
            if (await _Instance.NUMSUB(channel) > 0)
            {
                // Queue message only if any subscriber is waiting for it
                var str = JsonConvert.SerializeObject(message);
                await _Instance.GetDatabase().ListLeftPushAsync($"{_Prefix}{channel}.queue", str, flags: CommandFlags.FireAndForget);
                await _Instance.GetSubscriber().PublishAsync(channel, true);
            }
        }

        public async Task Register(string channel, WorkflowBase workflow)
        {
            await _Instance.GetSubscriber().SubscribeAsync(channel, async (chn, status) =>
            {
                var msg = await _Instance.GetDatabase().ListRightPopAsync($"{_Prefix}{channel}.queue");

                if (!msg.IsNullOrEmpty)
                {
                    try
                    {
                        var message = JsonConvert.DeserializeObject<Message>(msg);
                        await workflow.ExecuteAsync(message);
                    }
                    catch (Exception ex)
                    {
                        await Produce($"{_Prefix}WokrflowEvents", new OnExceptionMessage(ex));
                    }
                }
            });
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
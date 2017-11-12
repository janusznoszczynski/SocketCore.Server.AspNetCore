using System;
using System.Threading.Tasks;
using SocketCore.Server.AspNetCore;

namespace SocketCore.Server.AspNetCore.Tests
{
    public class LongRunningConnection : Connection
    {
        protected override async Task OnReceived(string connectionId, object data)
        {
            await Task.Delay(TimeSpan.FromSeconds(20));
            await SendToConnectionsAsync($"Reply to: {data}", connectionId);
        }
    }
}
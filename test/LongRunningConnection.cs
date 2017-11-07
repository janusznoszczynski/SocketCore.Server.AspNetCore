using System.Threading.Tasks;
using SocketCore.Server.AspNetCore;

namespace SocketCore.Server.AspNetCore.Tests
{
    public class LongRunningConnection : Connection
    {
        protected override Task OnReceived(string connectionId, object data)
        {
            return Task.Delay(10000).ContinueWith(t => SendToConnectionsAsync($"Reply to: {data}", connectionId));
        }
    }
}
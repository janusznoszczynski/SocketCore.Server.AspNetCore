using System.Threading.Tasks;
using SocketCore.Server.AspNetCore;

namespace SocketCore.Server.AspNetCore.Tests
{
    public class SimpleConnection : Connection
    {
        protected override Task OnReceived(string connectionId, object data)
        {
            return SendToConnectionsAsync($"Reply to: {data}", connectionId);
        }
    }
}
using System.Threading.Tasks;
using SocketCore.Server.AspNetCore;

namespace SocketCore.Server.AspNetCore.Tests
{
    public class SimpleConnection : Connection
    {
        protected override async Task OnReceived(string connectionId, object data)
        {
            await SendToConnectionsAsync($"Reply to: {data}", connectionId);
        }
    }
}
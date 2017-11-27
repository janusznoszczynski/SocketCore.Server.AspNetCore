using System;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SocketCore.Server.AspNetCore;


namespace SocketCore.Server.AspNetCore.Workflows.Messages
{
    public class OnDisconnectedMessage : Message
    {
        private static readonly string _Namespace = "SocketCore.WokrflowEvents";
        private static readonly string _Type = "OnDisconnectedMessage";

        public OnDisconnectedMessage(string connectionId)
            : base(_Namespace, _Type, connectionId)
        {
        }

        public static bool IsMatch(Message message)
        {
            return message.Namespace == _Namespace && message.Type == _Type;
        }

        public static string GetConnectionId(Message message)
        {
            return (string)message.Data;
        }
    }
}
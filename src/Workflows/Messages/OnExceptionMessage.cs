using System;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SocketCore.Server.AspNetCore;


namespace SocketCore.Server.AspNetCore.Workflows.Messages
{
    public class OnExceptionMessage : Message
    {
        private static readonly string _Namespace = "SocketCore.WokrflowEvents";
        private static readonly string _Type = "OnException";

        public OnExceptionMessage(Exception excepion)
            : base(_Namespace, _Type, excepion)
        {
        }

        public static bool IsMatch(Message message)
        {
            return message.Namespace == _Namespace && message.Type == _Type;
        }

        public static Exception GetException(Message message)
        {
            return (Exception)message.Data;
        }
    }
}
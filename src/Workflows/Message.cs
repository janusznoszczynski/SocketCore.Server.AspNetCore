using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using SocketCore.Server.AspNetCore;


namespace SocketCore.Server.AspNetCore.Workflows
{
    public class Message
    {
        public string Type { get; set; }
        public object Data { get; set; }
        public ICollection<MessageHeader> Headers { get; set; }


        public string MessageId { get; set; }
        public string ConnectionId { get; set; }
        public string SessionId { get; set; }
        public string SenderId { get; set; }
        public string ReplyToMessageId { get; set; }
    }


    public class MessageHeader
    {
        public string Name { get; set; }
        public string Value { get; set; }

        public MessageHeader()
        {
        }

        public MessageHeader(string name, string value)
        {
            if (string.IsNullOrEmpty(name))
            {
                throw new ArgumentException("Header name can not be null or empty");
            }

            Name = name;
            Value = value;
        }
    }
}
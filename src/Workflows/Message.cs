using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using SocketCore.Server.AspNetCore;


namespace SocketCore.Server.AspNetCore.Workflows
{
    public class Message
    {
        // Unique namespace for your messages.
        // Examples: MyNamespace, mycompany.com, root/aaa/bbb, aaa.bbb.ccc
        public string Namespace { get; set; }

        // Unique message type within your namespace
        // Examples: SaveUser, PlaceOrder, DisplayTaskList, DisplayValidationErrors
        public string Type { get; set; }

        // Arbitary payload
        public object Data { get; set; }

        // Metadata 
        public ICollection<MessageHeader> Headers { get; set; }


        // GUID         
        public string MessageId { get; set; }
        
        // Identifies web socket connection
        public string ConnectionId { get; set; }

        // Identifies client session, multiple connection ids may be inside one session (eg reconnections)
        public string SessionId { get; set; }

        // Identifies source of the message, eg MyBackend, MyFrontend
        public string SenderId { get; set; }

        // If message is reply to the other message this property indicates that message
        // Use message.Reply(...) method to create reply/replies
        public string ReplyToMessageId { get; set; }


        public Message()
        {
        }

        public Message(string ns, string type, object data)
        {
            Namespace = ns;
            Type = type;
            Data = data;
        }

        public Message(string ns, string type)
        : this(ns, type, null)
        {
        }


        public bool IsMatch(string nameSpace, string type)
        {
            return Namespace == nameSpace && Type == type;
        }

        public Message[] Reply(params Message[] replies)
        {
            if (null == replies)
            {
                return null;
            }

            foreach (var reply in replies)
            {
                Reply(reply);
            }

            return replies;
        }

        public Message Reply(Message reply)
        {
            if (null == reply)
            {
                return null;
            }

            reply.MessageId = Guid.NewGuid().ToString();
            reply.SessionId = SessionId;
            reply.ReplyToMessageId = ConnectionId;

            return reply;
        }
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
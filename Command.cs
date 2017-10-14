namespace SocketCore.Server.AspNetCore
{
    internal class Command
    {
        public string Type { get; set; }
        public object Data { get; set; }

        public Command()
        {
        }

        public Command(string type, object data)
        {
            Type = type;
            Data = data;
        }
    }
}
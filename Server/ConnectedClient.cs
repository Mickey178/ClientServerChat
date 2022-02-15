using System.Collections.Generic;
using System.Net.Sockets;
using System.Threading;
using MyStream;

namespace Server
{
    internal class ConnectedClient
    {
        public TcpClient Client { get; }

        public Queue<ChatMessageDto> Queue { get; }

        public AutoResetEvent Barrier { get; }

        public ConnectedClient(TcpClient client)
        {
            Client = client;
            Queue = new Queue<ChatMessageDto>();
            Barrier = new AutoResetEvent(false);
        }
    }
}

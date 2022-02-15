using System;
using System.Net;
using System.Net.Sockets;
using System.Collections.Generic;
using System.Threading.Tasks;
using MyStream;
using System.Timers;
using System.Runtime.InteropServices;

namespace Server
{
    internal class Program
    {
        private static readonly TcpListener listener = new TcpListener(IPAddress.Any, 8301);
        private static readonly List<ConnectedClient> clients = new List<ConnectedClient>();
        private static readonly object _SyncRoot = new object();

        public static void Main(string[] args)
        {
            listener.Start();

            Timer timer = new Timer(10000);
            timer.Elapsed += SendCurrentTimeToClients;
            timer.Start();

            while (true)
            {
                TcpClient tcpClient = listener.AcceptTcpClient();

                SocketKeepAliveMagic(tcpClient);

                var client = new ConnectedClient(tcpClient);
                clients.Add(client);

                Console.WriteLine($"New Connection: {tcpClient.Client.RemoteEndPoint}");
                RunReadThread(client);
                RunWriteThread(client);                    
            }
        }

        private static void SendCurrentTimeToClients(object sender, ElapsedEventArgs e)
        {
            string dateTime = DateTime.Now.ToLongTimeString();

            lock (_SyncRoot)
            {
                foreach (var item in clients)
                {
                    ChatMessageDto chatMessageDto = new ChatMessageDto { Name = "Server", Message = dateTime };
                    item.Queue.Enqueue(chatMessageDto);
                    item.Barrier.Set();
                }
            }
        }

        private static void RunReadThread(ConnectedClient connectedClient)
        {
            Task.Factory.StartNew(() =>
            {
                while(true)
                {
                    try
                    {
                        var myStreamReader = new MyStreamReaderAndWriter(connectedClient.Client);
                        var message = myStreamReader.Read();

                        lock (_SyncRoot)
                        {
                            foreach (var item in clients)
                            {
                                item.Queue.Enqueue(message);
                                item.Barrier.Set();
                            }
                        }

                        Console.WriteLine($"{message.Name}: {message.Message}");
                    }
                    catch (Exception ex)
                    {                       
                        Console.WriteLine(ex.Message);
                        clients.Remove(connectedClient);
                        return;
                    }
                }
            });
        }

        private static void RunWriteThread(ConnectedClient connectedClient)
        {
            Task.Factory.StartNew(() =>
            {
                while (true)
                {
                    if(connectedClient.Queue.Count == 0)
                    {
                        connectedClient.Barrier.WaitOne();
                    }

                    try
                    {
                        var myStreamReader = new MyStreamReaderAndWriter(connectedClient.Client);

                        var message = connectedClient.Queue.Dequeue();

                        myStreamReader.Write(message);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex.Message);
                        clients.Remove(connectedClient);
                        return;
                    }
                }
            });
        }

        private static void SocketKeepAliveMagic(TcpClient tcpClient)
        {
            int size = Marshal.SizeOf((uint)0);
            byte[] keepAlive = new byte[size * 3];

            Buffer.BlockCopy(BitConverter.GetBytes((uint)1), 0, keepAlive, 0, size);
            Buffer.BlockCopy(BitConverter.GetBytes((uint)5000), 0, keepAlive, size, size);
            Buffer.BlockCopy(BitConverter.GetBytes((uint)5000), 0, keepAlive, size * 2, size);

            tcpClient.Client.IOControl(IOControlCode.KeepAliveValues, keepAlive, null);
        }
    }
}

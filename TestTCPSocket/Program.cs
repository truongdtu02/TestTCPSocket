using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using NetCoreServer;

namespace TcpChatServer
{
    class ChatSession : TcpSession
    {
        public bool IsSending = false;
        public ChatSession(TcpServer server) : base(server) { }

        protected override void OnConnected()
        {
            Console.WriteLine($"Chat TCP session with Id {Id} connected!");

            // Send invite message
            //string message = "Hello from TCP chat! Please send a message or '!' to disconnect the client!";
            //SendAsync(message);
        }

        protected override void OnDisconnected()
        {
            Console.WriteLine($"Chat TCP session with Id {Id} disconnected!");
        }

        protected override void OnReceived(byte[] buffer, long offset, long size)
        {
            //string message = Encoding.UTF8.GetString(buffer, (int)offset, (int)size);
            //Console.WriteLine("Incoming: " + message);

            //// Multicast message to all connected sessions
            //Server.Multicast(message);

            //// If the buffer starts with '!' the disconnect the current session
            //if (message == "!")
            //    Disconnect();
        }

        protected override void OnError(SocketError error)
        {
            Console.WriteLine($"Chat TCP session caught an error with code {error}");
        }

        protected override void OnEmpty()
        {
            //Console.WriteLine($"ID {Id}, Pending byte: {BytesPending}, Sending bytes: {BytesSending}, Sent bytes: {BytesSent}");
        }
        public void TcpSessionSendAssync(byte[] sendBuff)
        {
            IsSending = true;
            SendAsync(sendBuff);
        }
    }

    class ChatServer : TcpServer
    {
        ChatSession chatSession;
        internal ChatSession ChatSession { get => chatSession; }

        internal ConcurrentDictionary<Guid, TcpSession> ListClient { get => Sessions; } 
        public ChatServer(IPAddress address, int port) : base(address, port) { }

        protected override TcpSession CreateSession() 
        {
            chatSession =  new ChatSession(this);
            return chatSession;
        }

        protected override void OnError(SocketError error)
        {
            Console.WriteLine($"Chat TCP server caught an error with code {error}");
        }       
    }

    class Program
    {
        static void Main(string[] args)
        {
            // TCP server port
            int port = 1308;
            //if (args.Length > 0)
            //    port = int.Parse(args[0]);

            Console.WriteLine($"TCP server port: {port}");

            Console.WriteLine();

            // Create a new TCP chat server
            var server = new ChatServer(IPAddress.Any, port);

            // Start the server
            Console.Write("Server starting...");
            server.Start();
            Console.WriteLine("Done!");

            //send buffer
            const int sendBuffSize = 40000;
            byte[] sendBuff = new byte[sendBuffSize];
            Random rdByte = new Random();
            rdByte.NextBytes(sendBuff);

            System.Buffer.BlockCopy(BitConverter.GetBytes(sendBuffSize), 0, sendBuff, 0, sizeof(int));

            Stopwatch sendWatch = new Stopwatch();
            sendWatch.Start();
            double dMarkTime = 0;
            int iMarktime = 0;
            const double interval = 500;
            // Perform text input
            for (; ; )
            {
                int count = 0;
                foreach (var session in server.ListClient.Values)
                {
                    System.Buffer.BlockCopy(BitConverter.GetBytes(iMarktime), 0, sendBuff, 4, sizeof(int));
                    if (session.BytesSending == 0 && session.BytesPending == 0)
                    {
                        //test
                        //if (count > 0)
                        //{
                        //    for(int j = 0; j < 30; j++)
                        //    {
                        //        session.SendAsync(sendBuff);
                        //    }
                        //}
                        //else
                        //{
                        //    session.SendAsync(sendBuff);
                        //}

                        session.SendAsync(sendBuff);

                    }
                    else
                    {
                        //missing
                        Console.Write(" + ");
                    }

                    count++;
                }

                iMarktime++;
                dMarkTime = interval * iMarktime - sendWatch.Elapsed.TotalMilliseconds;
                //Console.WriteLine(sendWatch.Elapsed.TotalMilliseconds);
                if ((int)dMarkTime > 0)
                    Thread.Sleep((int)dMarkTime);
                else
                {
                    Console.Write(" - ");
                }
            }

            // Stop the server
            Console.Write("Server stopping...");
            server.Stop();
            Console.WriteLine("Done!");
        }
    }
}

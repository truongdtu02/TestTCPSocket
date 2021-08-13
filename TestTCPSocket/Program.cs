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
        int totalBytes = 0;
        int countRecv = 0;
        long ConnectedTime;
        public ChatSession(TcpServer server) : base(server) { }

        protected override void OnConnected()
        {
            ConnectedTime = DateTimeOffset.Now.ToUnixTimeSeconds();
            Console.WriteLine($"{DateTimeOffset.Now} Chat TCP session with Id {Id} connected!");

            // Send invite message
            //string message = "Hello from TCP chat! Please send a message or '!' to disconnect the client!";
            //SendAsync(message);
        }

        protected override void OnDisconnected()
        {
            Console.WriteLine($"Chat TCP session with Id {Id} disconnected!");
        }

        const int TCpbuffLenght = 8000;
        const int packetTCPHeaderLength = 4;
        byte[] TCpbuff = new byte[TCpbuffLenght];
        int TCpbuffOffset = 0;
        int tcpPacketOfset;
        int oldIdTCp = 0;
        int missTCp = 0;

        bool bIsPending = false;
        bool bIgnore = false; //ignore when tcp packet length > mem
        int remainData = 0;   //reamin data need to collect
        //int totalByteLength = 0;
        void handle_recv_tcp_packet(byte[] tcpPacket, int offset, int length)
        {
            //totalByteLength += length;
            //static packetTCP* packetTCpheader = (packetTCP*)TCpbuff;
            
            tcpPacketOfset = offset;

            while (length > 0)
            {
                //beggin get length of packet
                if (!bIsPending)
                {
                    //barely occur, not enough data tp detect lenght of TCP packet
                    if ((TCpbuffOffset + length) < packetTCPHeaderLength)
                    {
                        System.Buffer.BlockCopy(TCpbuff, TCpbuffOffset, tcpPacket, tcpPacketOfset, length);
                        length = 0;
                        TCpbuffOffset += length;
                    }
                    //else enough data to detect
                    else
                    {
                        //copy just enough
                        int tmpOffset = packetTCPHeaderLength - TCpbuffOffset;
                        System.Buffer.BlockCopy(tcpPacket, tcpPacketOfset, TCpbuff, TCpbuffOffset, tmpOffset);
                        TCpbuffOffset = packetTCPHeaderLength;
                        length -= tmpOffset;
                        tcpPacketOfset += tmpOffset;
                        bIsPending = true;

                        remainData = TCpbuffLenght - packetTCPHeaderLength;

                        //not enough mem, so just ignore
                        //if(packetTCpheader->length > TCpbuffLenght)
                        //bIgnore = true;
                        //else
                        //  bIgnore = false;
                    }
                }
                //got length, continue collect data
                else
                {
                    //ignore save to buff
                    if (bIgnore)
                    {
                        if (length < remainData)
                        {
                            remainData -= length;
                            length = 0;
                        }
                        else
                        {
                            //done packet
                            length -= remainData;
                            tcpPacketOfset += remainData;
                            bIsPending = false;
                        }
                    }
                    //save to buff
                    else
                    {
                        //not enough data to get
                        if (length < remainData)
                        {
                            System.Buffer.BlockCopy(tcpPacket, tcpPacketOfset, TCpbuff, TCpbuffOffset, length);
                            TCpbuffOffset += length;
                            remainData -= length;
                            length = 0; //handled all data in tcpPacket
                        }
                        else
                        {
                            //done packet
                            System.Buffer.BlockCopy(tcpPacket, tcpPacketOfset, TCpbuff, TCpbuffOffset, remainData);
                            length -= remainData;
                            tcpPacketOfset += remainData;
                            bIsPending = false;
                        }
                    }

                    //that mean get done a packet
                    if (!bIsPending)
                    {
                        TCpbuffOffset = 0; //reset
                                           //check order
                        int curId = BitConverter.ToInt32(TCpbuff, 0);
                        if (curId > oldIdTCp)
                        {
                            missTCp += curId - oldIdTCp - 1;
                            if(curId > (oldIdTCp + 1))
                            {
                                //Console.WriteLine($"Recv {curId} , Miss {missTCp}");
                            }
                            oldIdTCp = curId;
                        }
                        Console.WriteLine($"Recv {curId} , Miss {missTCp} {DateTimeOffset.Now.ToUnixTimeSeconds() - ConnectedTime}");
                    }
                }
            }
        }

        protected override void OnReceived(byte[] buffer, long offset, long size)
        {
            string message = Encoding.UTF8.GetString(buffer, (int)offset, (int)size);
            Console.WriteLine("Incoming: " + message);

            //// Multicast message to all connected sessions
            //Server.Multicast(message);

            //// If the buffer starts with '!' the disconnect the current session
            //if (message == "!")
            //    Disconnect();
            totalBytes += (int)size;
            //handle_recv_tcp_packet(buffer, (int)offset, (int)size);
            if((totalBytes / 8000) > countRecv)
            {
                countRecv = totalBytes / 8000;
                Console.WriteLine($"R {totalBytes} {DateTimeOffset.Now.ToUnixTimeSeconds() - ConnectedTime} ");
            }
            //if (totalBytes == 8000)
            //{
            //    //Console.Write($"R {DateTimeOffset.Now.ToUnixTimeSeconds() - ConnectedTime} ");
            //    Console.WriteLine("R8000 ");
            //    totalBytes = 0;
            //}
            //else if(totalBytes > 8000)
            //{
            //    Console.WriteLine("R > 8000 ");
            //    totalBytes = 0;
            //}
            //Console.WriteLine($"R {totalBytes} {DateTimeOffset.Now.ToUnixTimeSeconds() - ConnectedTime} ");
        }

        protected override void OnError(SocketError error)
        {
            Console.WriteLine($"Chat TCP session caught an error with code {error}");
        }

        protected override void OnEmpty()
        {
            //Console.WriteLine($"S {++countSend} {DateTimeOffset.Now.ToUnixTimeSeconds() - ConnectedTime} ");
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
            const int sendBuffSize = 10000;
            byte[] sendBuff = new byte[sendBuffSize];
            Random rdByte = new Random();
            rdByte.NextBytes(sendBuff);

            System.Buffer.BlockCopy(BitConverter.GetBytes(sendBuffSize), 0, sendBuff, 0, sizeof(int));

            Stopwatch sendWatch = new Stopwatch();
            sendWatch.Start();
            double dMarkTime = 0;
            int iMarktime = 0;
            const double interval = 1000;
            // Perform text input
            int countTimeOut = 0;
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

                        //session.SendAsync(sendBuff);

                    }
                    else
                    {
                        //missing
                        countTimeOut++;
                        Console.Write(" + {0} ", countTimeOut);
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

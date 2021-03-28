using System;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace Client
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            Console.Title = "Tcp Client";
            IPAddress serverIP;
            // yêu cầu người dùng chon server
            Console.Write("Server: 1-local , 2-cloud");
            var choiceServer = Console.ReadKey();
            if(choiceServer.KeyChar == '1') {
                serverIP = IPAddress.Parse("127.0.0.1");
            }
            else
            {
                serverIP = IPAddress.Parse("45.118.145.137"); 
            }

            int serverPort = 1308;

            // đây là "địa chỉ" của tiến trình server trên mạng
            // mỗi endpoint chứa ip của host và port của tiến trình
            var serverEndpoint = new IPEndPoint(serverIP, serverPort);

            var size = 16000; // kích thước của bộ đệm
            var receiveBuffer = new byte[size]; // mảng byte làm bộ đệm            

            Stopwatch watch = new Stopwatch();

            // khởi tạo object của lớp socket để sử dụng dịch vụ Tcp
            // lưu ý SocketType của Tcp là Stream 
            var socket = new Socket(SocketType.Stream, ProtocolType.Tcp);
            double timepoint = 0, mark1, mark2;
            int frame = 0, delay = 0, LengthError = 0;
            while (true)
            {
                try
                {
                    // tạo kết nối tới Server
                    socket.Connect(serverEndpoint);
                    socket.ReceiveBufferSize = size;
                    watch.Start();
                }
                catch (Exception ex)
                {
                    Console.WriteLine (ex);
                    continue;
                }

                while (true)
                {
                    try
                    {
                        // nhận mảng byte từ dịch vụ Tcp và lưu vào bộ đệm
                        mark1 = watch.ElapsedMilliseconds;
                        var length = socket.Receive(receiveBuffer);
                        mark2 = watch.ElapsedMilliseconds;
                        frame++;
                        timepoint = mark2 - mark1;
                        if (timepoint > 1000)
                        {
                            delay++;
                        }
                        if (length != 16000) LengthError++;
                        //if (frame % 3 == 0)
                        {
                            Console.WriteLine("Frame: {0} , Delay: {1}, Time: {2}, LengthError: {3}", frame, delay, (int)mark2/1000, length);
                        }
                    }
                    catch
                    {

                    }
                }

            }
        }
    }
}
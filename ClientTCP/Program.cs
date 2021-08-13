using System;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

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
            IPEndPoint serverEndpoint = new IPEndPoint(serverIP, serverPort);

            int size = 10000; // kích thước của bộ đệm
            byte[] receiveBuffer = new byte[size]; // mảng byte làm bộ đệm            

            Stopwatch watch = new Stopwatch();

            // khởi tạo object của lớp socket để sử dụng dịch vụ Tcp
            // lưu ý SocketType của Tcp là Stream 
            Socket socket;// = new Socket(SocketType.Stream, ProtocolType.Tcp);
            double timepoint = 0, mark1, mark2;
            int frame = 0, delay = 0, LengthError = 0;

            byte[] sendBufferHello = Encoding.UTF8.GetBytes("hello");
            while (true)
            {
                try
                {
                    socket = new Socket(SocketType.Stream, ProtocolType.Tcp);
                    // tạo kết nối tới Server
                    socket.Connect(serverEndpoint);
                    socket.ReceiveBufferSize = size;
                    watch.Start();

                    while (socket.Connected)
                    {
                        try
                        {
                            //// nhận mảng byte từ dịch vụ Tcp và lưu vào bộ đệm
                            //mark1 = watch.ElapsedMilliseconds;
                            //var length = socket.Receive(receiveBuffer);
                            //mark2 = watch.ElapsedMilliseconds;
                            //frame++;
                            //timepoint = mark2 - mark1;
                            //if (timepoint >= 1000)
                            //{
                            //    delay++;
                            //}
                            ////if (length != 7000 && receiveBuffer[0] != 0xFF && receiveBuffer[1] != 0xFF) LengthError++;
                            ////if (frame % 3 == 0)
                            //{
                            //    //Console.WriteLine("Frame: {0} , Delay: {1}, Time: {2}, LengthError: {3}", frame, delay, (int)mark2 / 1000, length);
                            //}
                            socket.Send(sendBufferHello);
                            Thread.Sleep(1000);
                        }
                        catch
                        {

                        }
                    }
                    socket.Close();
                }
                catch (Exception ex)
                {
                    //Console.WriteLine (ex);
                    //socket.Close();
                    Thread.Sleep(1000);
                    //continue;
                }  
            }
        }
    }
}
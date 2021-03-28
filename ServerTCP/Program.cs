using System;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace Server
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            Console.Title = "Tcp Server";

            // giá trị Any của IPAddress tương ứng với Ip của tất cả các giao diện mạng trên máy
            IPAddress localIp = IPAddress.Any;
            // tiến trình server sẽ sử dụng cổng tcp 1308
            int localPort = 1308;
            // biến này sẽ chứa "địa chỉ" của tiến trình server trên mạng
            IPEndPoint localEndPoint = new IPEndPoint(localIp, localPort);

            // tcp sử dụng đồng thời hai socket: 
            // một socket để chờ nghe kết nối, một socket để gửi/nhận dữ liệu
            // socket listener này chỉ làm nhiệm vụ chờ kết nối từ Client
            Socket listener = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            // yêu cầu hệ điều hành cho phép chiếm dụng cổng tcp 1308
            // server sẽ nghe trên tất cả các mạng mà máy tính này kết nối tới
            // chỉ cần gói tin tcp đến cổng 1308, tiến trình server sẽ nhận được
            listener.Bind(localEndPoint);
            // bắt đầu lắng nghe chờ các gói tin tcp đến cổng 1308
            listener.Listen(10);
            Console.WriteLine($"Local socket bind to {localEndPoint}. Waiting for request ...");

            int size = 16000;
            byte[] sendBuffer = new byte[size];
            Random rand = new Random();
            for(int i = 0; i < size; i++)
            {
                sendBuffer[i] = (byte)rand.Next();
            }


            while (true)
            {
                // tcp đòi hỏi một socket thứ hai làm nhiệm vụ gửi/nhận dữ liệu
                // socket này được tạo ra bởi lệnh Accept
                Socket socket = listener.Accept();
                Console.WriteLine($"Accepted connection from {socket.RemoteEndPoint}");
                Stopwatch watch = new Stopwatch();
                
                const int frametime = 1000; //ms
                int Frame = 0;
                double timepoint = 0, marktime = 0, mark1, mark2;
                //socket.SendTimeout = 900;
                watch.Start();
                while (true)
                {
                    try
                    {
                        // gửi kết quả lại cho client
                        mark1 = watch.Elapsed.TotalMilliseconds;
                        socket.Send(sendBuffer);
                        Frame++;
                        marktime = Frame * frametime;
                        mark2 = watch.Elapsed.TotalMilliseconds;
                        timepoint = marktime - mark2;
                        Console.WriteLine("{0} , {1}", (int)(mark2 - mark1), timepoint);
                        if (timepoint > 0)
                        {
                            Thread.Sleep((int)timepoint);
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
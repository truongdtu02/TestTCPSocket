using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace TestTCPSocket
{
    internal class Program
    {
        Thread receiveThread;
        private static void Main(string[] args)
        {
            Console.Title = "Tcp Server";

            // giá trị Any của IPAddress tương ứng với Ip của tất cả các giao diện mạng trên máy
            var localIp = IPAddress.Any;
            // tiến trình server sẽ sử dụng cổng tcp 1308
            var localPort = 1308;
            // biến này sẽ chứa "địa chỉ" của tiến trình server trên mạng
            var localEndPoint = new IPEndPoint(localIp, localPort);

            // tcp sử dụng đồng thời hai socket: 
            // một socket để chờ nghe kết nối, một socket để gửi/nhận dữ liệu
            // socket listener này chỉ làm nhiệm vụ chờ kết nối từ Client
            var listener = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            // yêu cầu hệ điều hành cho phép chiếm dụng cổng tcp 1308
            // server sẽ nghe trên tất cả các mạng mà máy tính này kết nối tới
            // chỉ cần gói tin tcp đến cổng 1308, tiến trình server sẽ nhận được
            listener.Bind(localEndPoint);
            // bắt đầu lắng nghe chờ các gói tin tcp đến cổng 1308
            listener.Listen(10);
            Console.WriteLine($"Local socket bind to {localEndPoint}. Waiting for request ...");

            while (true)
            {
                // tcp đòi hỏi một socket thứ hai làm nhiệm vụ gửi/nhận dữ liệu
                // socket này được tạo ra bởi lệnh Accept
                Socket socket;
                try
                {
                    socket = listener.Accept();
                    if (socket.Connected)
                    {
                        Console.WriteLine($"Accepted connection from {socket.RemoteEndPoint}");
                        Thread thread = new Thread(() =>
                        {
                            Listenning(socket);
                        });
                        thread.Start();
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex);
                }          
            }       
            //// đóng kết nối và giải phóng tài nguyên
            //Console.WriteLine($"Closing connection from {socket.RemoteEndPoint}rn");
            //socket.Close();
        }

        private static void Listenning(Socket socket){
            int size = 1024;
            byte[] receiveBuffer = new byte[size];
            socket.ReceiveTimeout = 3000;
            int length;
            while (true)
            {         
                try
                {
                    // nhận dữ liệu vào buffer
                    length = socket.Receive(receiveBuffer);
                    socket.SendTimeout = 500;
                }
                catch
                {
                    socket.Close();
                    Console.WriteLine("Socket close!");
                    break;
                }                        
                var text = Encoding.ASCII.GetString(receiveBuffer, 0, length);
                Console.WriteLine(text);
            }
        }
    }
}
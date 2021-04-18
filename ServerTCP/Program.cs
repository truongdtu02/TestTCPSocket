using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using UDP_send_packet_frame;
using System.Linq;

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





            //Random rand = new Random();
            //for(int i = 0; i < size; i++)
            //{
            //    sendBuffer[i] = (byte)i;
            //}
            ////initialize
            //sendBuffer[0] = 0xFF;
            //sendBuffer[1] = 0x00;
            //sendBuffer[2] = 0xFF;


            List<soundTrack> soundListServer = new List<soundTrack>()
            {
                new soundTrack(){ FilePath = "bai11.mp3"},
                new soundTrack(){ FilePath = "bai22.mp3"}
                //new soundTrack(){ FilePath = "LoveIsBlue.mp3"}
            };

            List<soundTrack> soundList = new List<soundTrack>()
            {
                new soundTrack(){ FilePath = @"bai11.mp3"},
                new soundTrack(){ FilePath = @"E:\truyenthanhproject\code server C#\TestTCPSocket\TestTCPSocket\ServerTCP\bai22.mp3"}// E:\truyenthanhproject\code server C#\TestTCPSocket\TestTCPSocket\ServerTCP\ServerTCP.csproj
                //new soundTrack(){ FilePath = "LoveIsBlue.mp3"}
            };

            

            while (true)
            {
                // tcp đòi hỏi một socket thứ hai làm nhiệm vụ gửi/nhận dữ liệu
                // socket này được tạo ra bởi lệnh Accept
                //Socket socket = listener.Accept();
                //Console.WriteLine($"Accepted connection from {socket.RemoteEndPoint}");

                //sendPacket(socket, soundList);

                Socket socket;
                try
                {
                    socket = listener.Accept();
                    if (socket.Connected)
                    {
                        Console.WriteLine($"Accepted connection from {socket.RemoteEndPoint}");
                        Thread thread = new Thread(() =>
                        {
                            sendPacket(socket, soundList);
                        });
                        thread.Start();
                    }
                }
                catch (Exception ex)
                {
                    //Console.WriteLine(ex);
                }
            }
        }

        public static void sendPacket(Socket _socketSend, List<soundTrack> _soundList)
        {
            //int i = 1;
            //byte[] mp3_buff_tmp = File.ReadAllBytes(_soundList[i].FilePath);
            //byte[] mp3_buff = File.ReadAllBytes(_soundList[i].FilePath).Skip(237).ToArray();

            //sendPacketMP3(_socketSend, mp3_buff, mp3_buff.Length);
            //string hello = "hello";
            byte[] sendHello = Encoding.ASCII.GetBytes("hello");
            byte[] sendNum = new byte[255];
            for(int i = 0; i < 255; i++)
            {
                sendNum[i] = (byte)i;
            }
            sendNum[9] = (byte)111;
            while (true)
            {
                _socketSend.Send(sendNum);
                Thread.Sleep(2000);
            }
        }


        public static int sendPacketMP3(Socket _socketSend, byte[] mp3_buff, int mp3_buff_length)
        {

            double timePoint, mark_time = 0;

            MP3_frame mp3_reader = new MP3_frame(mp3_buff, mp3_buff_length);
            if (!mp3_reader.IsValidMp3())
            {
                return -1;
            }
            //const double framemp3_time = (double)1152.0 * 1000.0 / 44100.0; //ms
            double framemp3_time = mp3_reader.TimePerFrame_ms;
            //count total Frame of mp3
            //duration_song_s = mp3_reader.countFrame() * (int)mp3_reader.TimePerFrame_ms / 1000;

            const int FrameSend = 41;

            int numOfFrame, sizeOfPacket = FrameSend * 144 + 12;

            //int size = 7000;
            byte[] sendBuffer = new byte[sizeOfPacket];
            

            //SocketFlags socketFlag = new SocketFlags();

            //launch timer
            Stopwatch stopWatchSend = new Stopwatch();
            stopWatchSend.Start();

            while (true)
            {
                numOfFrame = packet_tcp_frameMP3(sendBuffer, mp3_reader, FrameSend);
                
                if (numOfFrame < FrameSend)
                {
                    sizeOfPacket = numOfFrame * 144 + 12;
                    byte[] sendBufferTMP = new byte[sizeOfPacket];
                    Buffer.BlockCopy(sendBuffer, 0, sendBufferTMP, 0, sizeOfPacket);
                    _socketSend.Send(sendBufferTMP);
                    Thread.Sleep(1000);
                    break;
                }

                _socketSend.Send(sendBuffer);

                mark_time += framemp3_time * numOfFrame; //point to next time frame
                //get current time playing
                //timePlaying_song_s = (int)mark_time / 1000; //second
                timePoint = mark_time - stopWatchSend.Elapsed.TotalMilliseconds;
                if ((int)timePoint > 0)
                {
                    Thread.Sleep((int)timePoint);
                }
            }
            //done a song
            stopWatchSend.Stop();
            return 0;
        }
        static private int packet_tcp_frameMP3(byte[] _send_buff, MP3_frame _mp3_reader, int _frameSend)
        {
            headerPacket HeaderPacket = new headerPacket();

            //reset packet header
            HeaderPacket.NumOffFrame = 0;
            HeaderPacket.TotalLength = HeaderPacket.Length;
            while (HeaderPacket.NumOffFrame < 41)
            {
                if (_mp3_reader.ReadNextFrame() == false) break;
                Buffer.BlockCopy(_mp3_reader.Mp3_buff, _mp3_reader.Start_frame, _send_buff, HeaderPacket.TotalLength, _mp3_reader.Frame_size);
                HeaderPacket.TotalLength += (UInt16)(_mp3_reader.Frame_size);
                HeaderPacket.NumOffFrame++;
            }
            if(HeaderPacket.NumOffFrame < 41)
            {
                Console.WriteLine("hh");
            }
            //copy header to _send_buff
            HeaderPacket.copyHeaderToBuffer(_send_buff);

            //sizeOfPacket = HeaderPacket.TotalLength;

            //increase IDframe
            //HeaderPacket.IDframe++;

            

            return HeaderPacket.NumOffFrame;
        }
    }

    class headerPacket
    {
        ////header of UDP packet
        //1-byte: volume, 1-byte: ID_song, 2-byte: totalLength
        //4-byte: ID_frame
        //2-byte: numOfFrame, 2-byte checksum
        //de cho an toan, nen tinh checksum cho header nay

        //total byte in header
        UInt16 length = 12;
        byte volume = 0x10; // max:min 0x00:0xFE
        byte id_song;
        UInt16 totalLength;
        UInt32 id_frame;
        UInt16 numOffFrame = 41;
        UInt16 checkSum;
        UInt16 checkSumData;

        public UInt16 Length { get => length; }

        internal byte IDsong { get => id_song; set => id_song = value; }
        internal ushort TotalLength { get => totalLength; set => totalLength = value; }
        internal uint IDframe { get => id_frame; set => id_frame = value; }
        internal ushort NumOffFrame { get => numOffFrame; set => numOffFrame = value; }

        internal byte Volume
        {
            get { return volume; }
            set
            {
                if (value == 0xFF)
                    volume = 0xFE;
                else
                    volume = value;
            }
        }

        internal void copyHeaderToBuffer(byte[] _buffer)
        {
            _buffer[0] = volume;
            _buffer[1] = id_song;
            byte[] tmp_byte = new byte[4];
            tmp_byte = BitConverter.GetBytes(totalLength);
            Buffer.BlockCopy(tmp_byte, 0, _buffer, 2, 2);
            tmp_byte = BitConverter.GetBytes(id_frame);
            Buffer.BlockCopy(tmp_byte, 0, _buffer, 4, 4);
            tmp_byte = BitConverter.GetBytes(numOffFrame);
            Buffer.BlockCopy(tmp_byte, 0, _buffer, 8, 2);

            //caculate checksum for header and checksum for data
            checkSum = caculateChecksum(_buffer, 0, length - 4); //header
            //checkSumData = caculateChecksum(_buffer, length, totalLength - length);

            tmp_byte = BitConverter.GetBytes(checkSum);
            Buffer.BlockCopy(tmp_byte, 0, _buffer, 10, 2);
            //tmp_byte = BitConverter.GetBytes(checkSumData);
            //Buffer.BlockCopy(tmp_byte, 0, _buffer, 12, 2);
        }

        static UInt16 caculateChecksum(byte[] data, int offset, int length)
        {
            UInt32 checkSum = 0;
            int index = offset;
            while (length > 1)
            {
                checkSum += ((UInt32)data[index] << 8) | ((UInt32)data[index + 1]); //little edian
                length -= 2;
                index += 2;
            }
            if (length == 1) // still have 1 byte
            {
                checkSum += ((UInt32)data[index] << 8);
            }
            while ((checkSum >> 16) > 0) //checkSum > 0xFFFF
            {
                checkSum = (checkSum & 0xFFFF) + (checkSum >> 16);
            }
            //inverse
            checkSum = ~checkSum;
            return (UInt16)checkSum;
        }
    }

}
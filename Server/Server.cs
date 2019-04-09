using KantanNetworking;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Server
{
    class Server
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Server Started");

            try
            {

                var server = new IoServer("127.0.0.1", 3000);

                server.OnConnection += (socket) =>
                {
                    Console.WriteLine($"{socket.EndPoint} : Connected.");

                    socket.OnReceive += (state, read) =>
                    {
                        Console.WriteLine($"Received {read} bytes from {socket.EndPoint}");
                    };
                    socket.OnDisconnect += (state) =>
                    {
                        Console.WriteLine($"{socket.EndPoint} : Disconnected.");
                    };
                    socket.OnSend += (ar, bytesSent) =>
                    {
                        Console.WriteLine($"Sent {bytesSent} bytes");
                    };

                    server.Send(server.Encoding.GetBytes("Client connected"));

                };

                //server.OnReceive += (state, read) =>
                //{
                //    Console.WriteLine($"Received {read} bytes from {state.Socket.EndPoint}");
                //};

                //server.OnDisconnect += (socket) =>
                //{
                //    Console.WriteLine($"{socket.EndPoint} : Disconnected.");
                //};

                server.StartListening();

                //server.Handler.Send(Encoding.ASCII.GetBytes("hello world"));

            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }

            Console.Read();
        }
    }
}

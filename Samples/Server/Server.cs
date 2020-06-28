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

                var server = new KantanServer("127.0.0.1", 3000);

                server.OnConnection += (socket) =>
                {
                    try
                    {
                        Console.WriteLine($"Socket {socket.Handler.EndPoint} Connected.");

                        socket.OnReceive += (message) =>
                        {
                            Console.WriteLine($"Received : {message.GetData<string>()} from : {socket.Handler.EndPoint}");
                        };

                        socket.OnDisconnect += (socket1) =>
                        {
                            Console.WriteLine($"Socket {socket.Handler.EndPoint} Disconnected.");
                        };

                        // Broadcast that a new Connection has been made.
                        server.Send("New Socket Connected");

                        // Send a message to the receiver.
                        socket.Send("Welcome");

                    }catch(Exception ex)
                    {
                        Console.WriteLine($"Exception caught on socket {socket.Handler.EndPoint} : \n {ex}");
                    }
                };

                server.OnReceive += (message) =>
                {
                    Console.WriteLine("Got some Unknown Message");
                };

                server.StartAsync();

                while (true)
                {
                    server.Send("To yall");
                    Console.ReadLine();
                }

            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }

            Console.Read();
        }
    }
}

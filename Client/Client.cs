using KantanNetworking;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace Client
{
    class Client
    {
        static void Main(string[] args)
        {

            test1();

        }

        public static void test1()
        {
            Console.WriteLine("Client Started");

            try
            {

                var client = new KantanSocket("127.0.0.1", 3000);

                client.OnConnection += (socket) =>
                {
                    Console.WriteLine("Connection Succesfull");
                };

                client.OnDisconnect += (socket) =>
                {
                    Console.WriteLine("Connection Lost");
                };

                client.OnReceive += (message) =>
                {
                    Console.WriteLine($"Received : {message}");
                };

                client.Connect();

                client.Send("OwO hewo");

            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }

            Console.Read();
        }
    }
}

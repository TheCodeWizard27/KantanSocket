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

                var client = new IoSocket("127.0.0.1", 3000);

                client.OnConnection += (socket) =>
                {
                    Console.WriteLine("Succesfully connected");
                };

                client.OnDisconnect += (socket) =>
                {
                    Console.WriteLine("Connection Lost");
                };

                client.OnReceive += (state, bytesRead) =>
                {
                    Console.WriteLine($"{bytesRead} bytes recieved.");
                };

                client.OnSend += (ar, bytesSent) =>
                {
                    Console.WriteLine($"Sent {bytesSent} bytes");
                };

                client.Connect();

                System.Threading.Thread.Sleep(1000);

                client.Send(Encoding.ASCII.GetBytes("test"));

            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }

            Console.Read();
        }
    }
}

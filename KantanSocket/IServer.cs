using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KantanNetworking
{

    public interface IServer : INetworking
    {

        void StartListening();
        Task StartListeningAsync();
        void StopListening();
        Task StopListeningAsync();

        void Send(ISocket socket, byte[] content);
        void Send(byte[] content);

    }
}

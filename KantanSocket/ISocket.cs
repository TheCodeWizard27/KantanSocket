using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KantanNetworking
{

    public delegate void OnConnectionGainedHandler();

    public interface ISocket : INetworking
    {

        void Send(byte[] content);

    }
}

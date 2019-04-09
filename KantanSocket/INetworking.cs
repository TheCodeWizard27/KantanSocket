using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace KantanNetworking
{

    public delegate void OnConnectionHandler(ISocket socket);
    public delegate void OnReceiveHandler(KantanState ks, int bytesRead);
    public delegate void OnSendHandler(IAsyncResult ar, int bytesSent);
    public delegate void OnDisconnectHandler(ISocket socket);

    public interface INetworking
    {
        /// <summary>
        /// Event that will be called once a connection has been made.
        /// </summary>
        event OnConnectionHandler OnConnection;

        /// <summary>
        /// Event that will be called once a message has been received.
        /// </summary>
        event OnReceiveHandler OnReceive;

        /// <summary>
        /// Event that will be called on sending a message.
        /// </summary>
        event OnSendHandler OnSend;

        /// <summary>
        /// Event that will be called on disconnection.
        /// </summary>
        event OnDisconnectHandler OnDisconnect;

        /// <summary>
        /// The remote EndPoint on which the socket will connect to.
        /// </summary>
        IPEndPoint EndPoint { get;}

        /// <summary>
        /// The Socket that will be used for the Connection.
        /// </summary>
        Socket Handler { get;}

        /// <summary>
        /// The Encoding used for string conversions.
        /// </summary>
        Encoding Encoding { get; set; }

        /// <summary>
        /// The BufferSize used for transfering and buffering bytes.
        /// </summary>
        KantanBufferSize BufferSize { get; set; }

    }
}

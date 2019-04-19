using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace KantanNetworking
{
    public class IoServer : IServer
    {

        private ManualResetEvent _listeningEvent = new ManualResetEvent(false);
        private bool _isListening = false;


        #region Constructor

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="ip">The Ip Address to connect to.</param>
        /// <param name="port">The port to connect to.</param>
        /// <param name="listenBackLog">(optional) The maximum lenght of the pending connections queue. | Default = 100</param>
        public IoServer(string ip, int port, int listenBackLog = 100) : this(IPAddress.Parse(ip),port, listenBackLog)
        {
        }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="ip">The Ip Address to connect to.</param>
        /// <param name="port">The port to connect to.</param>
        /// <param name="listenBackLog">(optional) The maximum lenght of the pending connections queue. | Default = 100</param>
        public IoServer(IPAddress ip, int port, int listenBackLog = 100)
        {
            EndPoint = new IPEndPoint(ip, port);
            Handler = new Socket(EndPoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
            Handler.Bind(EndPoint);
            Handler.Listen(listenBackLog);
        }

        #endregion


        #region Properties

        public IPEndPoint EndPoint { get; private set; }
        public Socket Handler { get; private set; }
        public Encoding Encoding { get; set; } = Encoding.ASCII;
        public KantanBufferSize BufferSize { get; set; } = KantanBufferSize.Default;

        /// <summary>
        /// List of sockets that have are connected to the server.
        /// </summary>
        public List<ISocket> ConnectedSockets { get; private set; } = new List<ISocket>();

        #endregion


        #region Events

        public event OnConnectionHandler OnConnection;
        public event OnDisconnectHandler OnDisconnect;
        public event OnReceiveHandler OnReceive;
        public event OnSendHandler OnSend;

        #endregion


        #region Methods

        /// <summary>
        /// Send message to a socket.
        /// </summary>
        /// <param name="socket">The socket used for sending.</param>
        /// <param name="content">The byte array that will be sent.</param>
        public void Send(ISocket socket, byte[] content)
        {
            socket.Handler.BeginSend(content, 0, content.Length, 0, (ar) =>
            {
                var bytesSent = socket.Handler.EndSend(ar);

                OnSend?.Invoke(ar, bytesSent);
                ((IoSocket)socket)?.Send(ar, bytesSent);
            }, socket.Handler);
        }

        /// <summary>
        /// Broadcasts a message to every connected socket.
        /// </summary>
        /// <param name="content">The byte array that will be sent.</param>
        public void Send(byte[] content)
        {
            foreach (var socket in ConnectedSockets)
                Send(socket, content);
        }

        /// <summary>
        /// Starts Listening to connections and messages.
        /// </summary>
        public void StartListening()
        {
            _isListening = true;
            while (_isListening)
            {
                _listeningEvent.Reset();

                Handler.BeginAccept((ar) =>
                {
                    _listeningEvent.Set();

                    var tmpSocket = Handler.EndAccept(ar);
                    var tmpState = new KantanState(new IoSocket(tmpSocket, (IPEndPoint) tmpSocket.RemoteEndPoint), BufferSize);

                    OnConnection?.Invoke(tmpState.Socket);
                    ConnectedSockets.Add(tmpState.Socket);

                    tmpSocket.BeginReceive(tmpState.Buffer, 0, (int)tmpState.BufferSize, 0, new AsyncCallback(ReceiveCallBack), tmpState);

                }, Handler);

                // Wait for a connection to happen.
                _listeningEvent.WaitOne();
            }
        }

        /// <summary>
        /// Starts Listening to connections and messages without blocking.
        /// </summary>
        /// <returns></returns>
        public async Task StartListeningAsync()
        {
            await Task.Run(() =>
            {
                StartListening();
            });
        }

        /// <summary>
        /// Stops Listening to connections and messages.
        /// </summary>
        public void StopListening()
        {
            _isListening = false;
            _listeningEvent.Set();
        }

        /// <summary>
        /// Protected Method that will be called upon receiving data.
        /// This Method will be called in a loop upon connection and will end upon losing the connection.
        /// </summary>
        /// <param name="ar"></param>
        protected void ReceiveCallBack(IAsyncResult ar)
        {
            var state = (KantanState)ar.AsyncState;

            try
            {
                int bytesRead = state.Socket.Handler.EndReceive(ar);

                if(bytesRead > 0)
                {

                    ((IoSocket)state.Socket)?.Receive(state, bytesRead); // Call Receive Event on the socket.
                    OnReceive?.Invoke(state, bytesRead); // Call Receive Event on the server.
                }

                state.Socket.Handler.BeginReceive(state.Buffer, 0, (int)state.BufferSize, 0, ReceiveCallBack, state);
            }
            catch (Exception)
            {
                OnDisconnect?.Invoke(state.Socket); // Call Disconnect event on the server.
                ((IoSocket)state.Socket)?.LoseConnection(); // Call Disconnect event on socket.
                ConnectedSockets.Remove(state.Socket);
            }
        }

        #endregion

    }
}

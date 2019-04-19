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

    public class IoSocket : ISocket
    {

        #region Constructor

        /// <summary>
        /// Internal Constructor, used by the server 
        /// for creating an IoSocket with an already existing socket.
        /// </summary>
        /// <param name="socket">Already existing Socket</param>
        /// <param name="endPoint">Remote EndPoint of the Socket</param>
        internal IoSocket(Socket socket, IPEndPoint endPoint)
        {
            Handler = socket;
            EndPoint = endPoint;
        }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="ip">The Ip Address to connect to.</param>
        /// <param name="port">The port to connect to.</param>
        public IoSocket(string ip, int port) : this(IPAddress.Parse(ip), port)
        {
        }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="ip">The Ip Address to connect to.</param>
        /// <param name="port">The port to connect to.</param>
        public IoSocket(IPAddress ip, int port)
        {
            EndPoint = new IPEndPoint(ip, port);
            Handler = new Socket(EndPoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
        }

        #endregion


        #region Properties

        public IPEndPoint EndPoint { get; private set; }
        public Socket Handler { get; private set; }

        public Encoding Encoding { get; set; } = Encoding.ASCII;
        public KantanBufferSize BufferSize { get; set; } = KantanBufferSize.Default;

        #endregion


        #region Events

        public event OnConnectionHandler OnConnection;
        public event OnReceiveHandler OnReceive;
        public event OnSendHandler OnSend;
        public event OnDisconnectHandler OnDisconnect;

        #endregion


        #region Methods

        /// <summary>
        /// Send a byte array to remote server.
        /// </summary>
        /// <param name="content">The byte array that will be sent.</param>
        public void Send(byte[] content)
        {
           
            Handler.BeginSend(content, 0, content.Length, 0, (ar) =>
            {
                OnSend?.Invoke(ar, Handler.EndSend(ar));
            }, Handler);
            
        }

        /// <summary>
        /// Internal Method for remote calls on the send event.
        /// </summary>
        /// <param name="ar">The result of the begin send method.</param>
        /// <param name="bytesSent">The return value of EndSend stating how many bytes have been sent.</param>
        internal void Send(IAsyncResult ar, int bytesSent)
        {
            OnSend?.Invoke(ar, bytesSent);
        }

        /// <summary>
        /// Internal Method for remote calls on the receive event.
        /// </summary>
        /// <param name="state">The state passed on receiving.</param>
        /// <param name="bytesRead">The return value of EndReceive stating how many bytes have been read.</param>
        internal void Receive(KantanState state, int bytesRead)
        {
            OnReceive?.Invoke(state, bytesRead);
        }

        /// <summary>
        /// Internal Method for remote calls on the Disconnect event.
        /// </summary>
        internal void LoseConnection()
        {
            OnDisconnect?.Invoke(this);
        }

        /// <summary>
        /// Protected Method that will be called upon receiving data.
        /// This Method will be called in a loop upon connection and will end upon losing the connection.
        /// </summary>
        /// <param name="ar"></param>
        protected void ReceiveCallBack(IAsyncResult ar)
        {
            try
            {
                var state = (KantanState)ar.AsyncState;
                var bytesRead = state.Socket.Handler.EndReceive(ar);

                if (bytesRead > 0)
                {

                    OnReceive?.Invoke(state, bytesRead);
                }

                Handler.BeginReceive(state.Buffer, 0, (int)state.BufferSize, 0, new AsyncCallback(ReceiveCallBack), state);
            }
            catch (Exception)
            {
                OnDisconnect?.Invoke(this);
            }
        }

        /// <summary>
        /// Will try to connect to server defined in constructor.
        /// </summary>
        public void Connect()
        {
            // Buffer Exception in Callback for Execution.
            Exception bufferedException = null;

            var connectionEvent = new ManualResetEvent(false);
            connectionEvent.Reset();

            Handler.BeginConnect(EndPoint, (ar) =>
            {
                try
                {
                    Handler.EndConnect(ar);

                    var state = new KantanState(this, BufferSize);

                    OnConnection?.Invoke(this);

                    Handler.BeginReceive(state.Buffer, 0, (int)state.BufferSize, 0, new AsyncCallback(ReceiveCallBack), state);
                }catch(Exception ex)
                {
                    bufferedException = ex;
                }
                finally
                {
                    // Set event to unblock the connect function
                    connectionEvent.Set();
                }
            }, Handler);

            // Wait until connection has been made.
            connectionEvent.WaitOne(); 

            // Throw Exception Caught in CallBack.
            if (bufferedException != null) throw bufferedException;
        }

        /// <summary>
        /// Will try to connect to server defined in constructor without blocking.
        /// </summary>
        /// <returns></returns>
        public async Task ConnectAsync()
        {
            await Task.Run(() =>
            {
                Connect();
            });
        }

        #region Static

        /// <summary>
        /// Will create a IoSocket with an already existing connection.
        /// </summary>
        /// <param name="ip">The Ip Address to connect to.</param>
        /// <param name="port">The port to connect to.</param>
        /// <param name="encoding">(optional) defines what encoding will be used | Default = ASCII</param>
        /// <param name="bufferSize">(optional) defines what buffer size will be used | Default = KantanBufferSize.Default</param>
        /// <returns>Connected IoSocket.</returns>
        public static IoSocket ConnectTo(IPAddress ip, int port, Encoding encoding = null, KantanBufferSize bufferSize = KantanBufferSize.Default)
        {
            // Create IoSocket object.
            var ioSocket = new IoSocket(ip, port)
            {
                Encoding = encoding ?? Encoding.ASCII,
                BufferSize = bufferSize
            };

            // Connect synchronously.
            ioSocket.Connect();

            return ioSocket;
        }

        /// <summary>
        /// Will create a IoSocket with an already existing connection without blocking.
        /// </summary>
        /// <param name="ip">The Ip Address to connect to.</param>
        /// <param name="port">The port to connect to.</param>
        /// <param name="encoding">(optional) defines what encoding will be used | Default = ASCII</param>
        /// <param name="bufferSize">(optional) defines what buffer size will be used | Default = KantanBufferSize.Default</param>
        /// <returns>Connected IoSocket.</returns>
        public static async Task<IoSocket> ConnectToAsync(IPAddress ip, int port, Encoding encoding = null, KantanBufferSize bufferSize = KantanBufferSize.Default)
        {
            return await Task.Run(() =>
            {
                return ConnectTo(ip, port, encoding, bufferSize);
            });
        }

        /// <summary>
        /// Will create a IoSocket with an already existing connection.
        /// </summary>
        /// <param name="ip">The Ip Address to connect to.</param>
        /// <param name="port">The port to connect to.</param>
        /// <param name="encoding">(optional) defines what encoding will be used | Default = ASCII</param>
        /// <param name="bufferSize">(optional) defines what buffer size will be used | Default = KantanBufferSize.Default</param>
        /// <returns>Connected IoSocket.</returns>
        public static IoSocket ConnectTo(string ip, int port, Encoding encoding = null, KantanBufferSize bufferSize = KantanBufferSize.Default)
        {
            return ConnectTo(IPAddress.Parse(ip), port, encoding, bufferSize);
        }

        /// <summary>
        /// Will create a IoSocket with an already existing connection without blocking.
        /// </summary>
        /// <param name="ip">The Ip Address to connect to.</param>
        /// <param name="port">The port to connect to.</param>
        /// <param name="encoding">(optional) defines what encoding will be used | Default = ASCII</param>
        /// <param name="bufferSize">(optional) defines what buffer size will be used | Default = KantanBufferSize.Default</param>
        /// <returns>Connected IoSocket.</returns>
        public static async Task<IoSocket> ConnectToAsync(string ip, int port, Encoding encoding = null, KantanBufferSize bufferSize = KantanBufferSize.Default)
        {
            return await ConnectToAsync(IPAddress.Parse(ip), port, encoding, bufferSize);
        }

        #endregion

        #endregion

    }
}

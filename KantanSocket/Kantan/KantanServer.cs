using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KantanNetworking
{
    public class KantanServer
    {

        #region Constructors

        public KantanServer(string ip, int port, int listenBacklog = 100)
        {
            Handler = new IoServer(ip, port, listenBacklog = 100);
            Encoding = Encoding.ASCII;
            InitEventRedirecters();
        }

        #endregion


        #region Properties

        public IServer Handler { get; private set; }
        public Encoding Encoding
        {
            get { return Handler?.Encoding; }
            set { Handler.Encoding = value; }
        }

        public string EndOfMessage { get; set; } = "<EOF>";

        private Dictionary<ISocket, KantanSocket> SocketMap { get; set; } = new Dictionary<ISocket, KantanSocket>();

        #endregion


        #region Events

        public delegate void OnConnectionHandler(KantanSocket socket);
        public delegate void OnDisconnectHandler(KantanSocket socket);
        public delegate void OnReceiveHandler(string message);

        public event OnConnectionHandler OnConnection;
        public event OnDisconnectHandler OnDisconnect;
        public event OnReceiveHandler OnReceive;

        #endregion


        #region Methods

        public void Start()
        {
            Handler.StartListening();
        }

        public async Task StartAsync()
        {
            await Handler.StartListeningAsync();
        }

        public void Stop()
        {
            Handler.StopListening();
        }

        public async Task StopAsync()
        {
            await Handler.StopListeningAsync();
        }

        public void Send(object message)
        {
            var tmpMsg = Encoding.GetBytes(JsonConvert.SerializeObject(message) + EndOfMessage);

            foreach (var kvp in SocketMap)
                kvp.Key.Send(tmpMsg);
        }

        public void Send(string message)
        {
            var tmpMsg = Encoding.GetBytes(message + EndOfMessage);

            foreach (var kvp in SocketMap)
                kvp.Key.Send(tmpMsg);
        }

        public void Send(KantanSocket socket, object message)
        {
            socket.Send(message);
        }

        public void Send(KantanSocket socket, string message)
        {
            socket.Send(message);
        }

        private void InitEventRedirecters()
        {
            Handler.OnConnection += Handler_OnConnection;
            Handler.OnDisconnect += Handler_OnDisconnect;
            Handler.OnReceive += Handler_OnReceive;
        }

        private void Handler_OnReceive(KantanState ks, int bytesRead)
        {
            ks.StringBuffer.Append(Encoding.GetString(ks.Buffer, 0, bytesRead));

            var tmpString = ks.StringBuffer.ToString();

            if(tmpString.IndexOf(EndOfMessage) > -1)
            {
                OnReceive?.Invoke(tmpString.Replace(EndOfMessage, ""));

                ks.ClearBuffer();
            }

        }

        private void Handler_OnDisconnect(ISocket socket)
        {
            var tempSocket = SocketMap[socket];
            OnDisconnect?.Invoke(tempSocket);
            SocketMap.Remove(socket);
        }

        private void Handler_OnConnection(ISocket socket)
        {
            var tempSocket = new KantanSocket(socket, socket.Encoding)
            {
                EndOfMessage = EndOfMessage,
                Encoding = Encoding
            };

            OnConnection?.Invoke(tempSocket);
            SocketMap.Add(socket, tempSocket);
        }

        #endregion

    }
}

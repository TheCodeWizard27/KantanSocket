using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace KantanNetworking
{
    public class KantanSocket
    {

        #region Constructors

        internal KantanSocket(ISocket handler, Encoding encoding = null)
        {
            Handler = handler;
            Encoding = encoding ?? Encoding.ASCII;
            InitEventRedirecters();
        }

        public KantanSocket(string ip, int port, Encoding encoding = null)
        {
            Handler = new IoSocket(ip, port);
            Encoding = encoding ?? Encoding.ASCII;
            InitEventRedirecters();
        }

        #endregion


        #region Properties

        public ISocket Handler { get; private set; }
        public Encoding Encoding
        {
            get { return Handler.Encoding; }
            set { Handler.Encoding = value; }
        }

        public string EndOfMessage { get; set; } = "<EOF>";

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

        public void Connect()
        {
            Handler.Connect();
        }

        public async Task ConnectAsync()
        {
            await Handler.ConnectAsync();
        }

        public void Send(object message)
        {
            Handler.Send(Encoding.GetBytes(JsonConvert.SerializeObject(message) + EndOfMessage));
        }

        public void Send(string message)
        {
            Handler.Send(Encoding.GetBytes(message + EndOfMessage));
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

            if (tmpString.IndexOf(EndOfMessage) > -1)
            {
                OnReceive?.Invoke(tmpString.Replace(EndOfMessage, ""));

                ks.ClearBuffer();
            }
        }

        private void Handler_OnDisconnect(ISocket socket)
        {
            OnDisconnect?.Invoke(this);
        }

        private void Handler_OnConnection(ISocket socket)
        {
            OnConnection?.Invoke(this);
        }

        #endregion

    }
}

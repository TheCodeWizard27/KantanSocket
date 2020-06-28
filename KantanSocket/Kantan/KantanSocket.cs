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
        public string EndOfConnection { get; set; } = "<EOFC>";
        private List<ChannelSubscription> _subscriptions { get; set; } = new List<ChannelSubscription>();

        #endregion


        #region Events

        public delegate void OnConnectionHandler(KantanSocket socket);
        public delegate void OnDisconnectHandler(KantanSocket socket);
        public delegate void OnReceiveHandler(NetworkMessage message);

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

        public void Disconnect()
        {
            Send(EndOfConnection);
            Handler.Disconnect();
        }

        public async Task DisconnectAsync()
        {
            await Handler.DisconnectAsync();
        }

        public void Send(object message) => Send("", message);
        public void Send(string channel, object message) => Handler.Send(PrepareMessage(channel, message));
        public ChannelSubscription Subscribe(string channel, Action<NetworkMessage> action)
        {
            var tmpSubscription = new ChannelSubscription()
            {
                Channel = channel,
                Action = action
            };
            _subscriptions.Add(tmpSubscription);
            return tmpSubscription;
        }
        public void Unsubscribe(ChannelSubscription sub) => _subscriptions.Remove(sub);

        private byte[] PrepareMessage(string channel, object message)
        {
            return Encoding.GetBytes(
                JsonConvert.SerializeObject(new NetworkMessage()
                {
                    Channel = channel,
                    Data = message
                }) + EndOfMessage);
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
                if (tmpString.IndexOf(EndOfConnection) != -1)
                {
                    Disconnect();
                    return;
                }
                tmpString = tmpString.Replace(EndOfMessage, "");
                HandleReceived(tmpString);
                ks.ClearBuffer();
            }
        }
        private void HandleReceived(string receivedString)
        {
            var tmpMessage = JsonConvert.DeserializeObject<NetworkMessage>(receivedString);
            OnReceive?.Invoke(tmpMessage);
            foreach(var sub in _subscriptions.Where(sub => sub.Channel == tmpMessage.Channel)) {
                sub.Action(tmpMessage);
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

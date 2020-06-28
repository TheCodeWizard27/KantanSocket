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
        public string EndOfConnection { get; set; } = "<EOFC>";

        private Dictionary<ISocket, KantanSocket> _socketMap { get; set; } = new Dictionary<ISocket, KantanSocket>();
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


        #region Public Methods

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

        public void Send(object message) => Send("", message);
        public void Send(string channel, object message)
        {
            var tmpMsg = PrepareMessage(channel, message);

            foreach (var kvp in _socketMap)
                kvp.Key.Send(tmpMsg);
        }

        public void Send(ISocket socket, object message) => Send(socket, "", message);
        public void Send(ISocket socket, string channel, object message) => socket.Send(PrepareMessage(channel, message));

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

        #endregion

        #region Private Methods

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

            if(tmpString.IndexOf(EndOfMessage) > -1)
            {
                HandleReceived(tmpString.Replace(EndOfMessage, ""));
                ks.ClearBuffer();
            }

        }

        private void HandleReceived(string receivedString)
        {
            var tmpMessage = JsonConvert.DeserializeObject<NetworkMessage>(receivedString);
            OnReceive?.Invoke(tmpMessage);
            foreach (var sub in _subscriptions.Where(sub => sub.Channel == tmpMessage.Channel))
            {
                sub.Action(tmpMessage);
            }
        }

        private void Handler_OnDisconnect(ISocket socket)
        {
            var tempSocket = _socketMap[socket];
            OnDisconnect?.Invoke(tempSocket);
            _socketMap.Remove(socket);
        }

        private void Handler_OnConnection(ISocket socket)
        {
            var tempSocket = new KantanSocket(socket, socket.Encoding)
            {
                EndOfMessage = EndOfMessage,
                Encoding = Encoding
            };

            OnConnection?.Invoke(tempSocket);
            _socketMap.Add(socket, tempSocket);
        }

        #endregion

    }
}

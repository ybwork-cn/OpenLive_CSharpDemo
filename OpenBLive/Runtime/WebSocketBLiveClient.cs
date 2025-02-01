using OpenBLive.Client.Data;
using OpenBLive.Runtime.Data;
using System.Net.WebSockets;
using Websocket.Client;

namespace OpenBLive.Runtime
{
    public class WebSocketBLiveClient : BLiveClient
    {

        /// <summary>
        ///  wss 长连地址
        /// </summary>
        public IList<string> WssLink;

        public WebsocketClient clientWebSocket;

        public WebSocketBLiveClient(AppStartInfo info)
        {
            var websocketInfo = info.Data.WebsocketInfo;

            WssLink = websocketInfo.WssLink;
            token = websocketInfo.AuthBody;
        }

        public WebSocketBLiveClient(IList<string> wssLink, string authBody)
        {
            WssLink = wssLink;
            token = authBody;
        }


        public override async void Connect()
        {
            var url = WssLink.FirstOrDefault();
            if (string.IsNullOrEmpty(url))
            {
                throw new Exception("wsslink is invalid");
            }

            Disconnect();

            clientWebSocket = new WebsocketClient(new Uri(url));
            clientWebSocket.MessageReceived.Subscribe(e =>
            ProcessPacket(e.Binary));
            clientWebSocket.DisconnectionHappened.Subscribe(e =>
            {
                if (e.CloseStatus == WebSocketCloseStatus.Empty)
                    Console.WriteLine("WS CLOSED");
                else
                    Console.WriteLine("WS ERROR: " + e.Exception.Message);
            });

            await clientWebSocket.Start();
            if (clientWebSocket.IsStarted)
                OnOpen();
        }

        /// <summary>
        /// 带有重连
        /// </summary>
        /// <param name="timeout">ReconnectTimeout ErrorReconnectTimeout</param>
        public override async void Connect(TimeSpan timeout)
        {
            var url = WssLink.FirstOrDefault();
            if (string.IsNullOrEmpty(url))
                throw new Exception("wsslink is invalid");

            clientWebSocket?.Stop(WebSocketCloseStatus.Empty, string.Empty);
            clientWebSocket?.Dispose();

            clientWebSocket = new WebsocketClient(new Uri(url));
            clientWebSocket.MessageReceived.Subscribe(e =>
            {
                //Console.WriteLine(e.Binary.Length);
                ProcessPacket(e.Binary);
            });
            clientWebSocket.DisconnectionHappened.Subscribe(e =>
            {
                if (e.CloseStatus == WebSocketCloseStatus.Empty)
                    Console.WriteLine("WS CLOSED");
                else if (e?.Exception != null)
                    Console.WriteLine("WS ERROR: " + e?.Exception?.Message);
            });
            await clientWebSocket.Start();
            clientWebSocket.IsReconnectionEnabled = true;
            clientWebSocket.ReconnectTimeout = timeout;
            clientWebSocket.ErrorReconnectTimeout = timeout;
            clientWebSocket.ReconnectionHappened.Subscribe(e =>
            {
                SendAsync(Packet.Authority(token));
            });
            if (clientWebSocket.IsStarted)
                OnOpen();
        }

        public override void Disconnect()
        {
            clientWebSocket?.Stop(WebSocketCloseStatus.Empty, string.Empty);
            clientWebSocket?.Dispose();
        }

        public override void Dispose()
        {
            Disconnect();
            GC.SuppressFinalize(this);
        }

        public override void Send(byte[] packet)
        {
            clientWebSocket?.Send(packet);
        }


        public override void Send(Packet packet) => Send(packet.ToBytes);
        public override Task SendAsync(byte[] packet) => Task.Run(() => Send(packet));
        protected override Task SendAsync(Packet packet) => SendAsync(packet.ToBytes);
    }
}

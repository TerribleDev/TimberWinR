using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Net;
using System.Net.Sockets;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NLog;

namespace TimberWinR.Inputs
{
    public class UdpInputListener : InputListener
    {
        private readonly System.Net.Sockets.UdpClient _udpListener;
        private IPEndPoint groupV4;
        private IPEndPoint groupV6;

        private Thread _listenThreadV4;
        private Thread _listenThreadV6;
     
        private readonly int _port;
        private long _receivedMessages;

        private struct listenProfile
        {
            public IPEndPoint endPoint;
            public UdpClient client;
        }    

        public override JObject ToJson()
        {
            JObject json = new JObject(
                new JProperty("udp",
                    new JObject(
                        new JProperty("port", _port),
                        new JProperty("messages", _receivedMessages)
                        )));

            return json;
        }

        public UdpInputListener(CancellationToken cancelToken, int port = 5140)
            : base(cancelToken, "Win32-Udp")
        {
            _port = port;

            LogManager.GetCurrentClassLogger().Info("Udp Input on Port {0} Ready", _port);

            _receivedMessages = 0;

            _udpListener = new System.Net.Sockets.UdpClient(port);

            _listenThreadV4 = new Thread(new ParameterizedThreadStart(StartListener));
            _listenThreadV4.Start(new listenProfile() {endPoint = groupV4, client = _udpListener});

            _listenThreadV6 = new Thread(new ParameterizedThreadStart(StartListener));
            _listenThreadV6.Start(new listenProfile() { endPoint = groupV6, client = _udpListener });   
        }


        public override void Shutdown()
        {                     
            Finished();
            base.Shutdown();
        }


        private void StartListener(object useProfile)
        {
            var profile = (listenProfile)useProfile;

            try
            {
                while (!CancelToken.IsCancellationRequested)
                {
                    byte[] bytes = profile.client.Receive(ref profile.endPoint);
                    var data = Encoding.ASCII.GetString(bytes, 0, bytes.Length);
                    JObject json = JObject.Parse(data);
                    ProcessJson(json);
                    _receivedMessages++;
                }
                _udpListener.Close();
            }
            catch (Exception ex)
            {
                LogManager.GetCurrentClassLogger().Error(ex);
            }

            Finished();
        }
    }
}

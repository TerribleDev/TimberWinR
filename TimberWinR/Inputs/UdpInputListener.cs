using System;
using System.Text;
using System.Threading;
using System.Net;
using System.Net.Sockets;
using Newtonsoft.Json.Linq;
using NLog;

namespace TimberWinR.Inputs
{
    public class UdpInputListener : InputListener
    {
        private readonly UdpClient _udpListener;

        private readonly Thread _listenThreadV4;
        private readonly Thread _listenThreadV6;

        private readonly int _port;
        private long _receivedMessages;
        private long _parsedErrors;

        private struct ListenProfile
        {
            public IPEndPoint EndPoint;
            public UdpClient Client;
        }

        public override JObject ToJson()
        {
            var json = new JObject(
                new JProperty("udp",
                    new JObject(
                        new JProperty("port", _port),
                        new JProperty("errors", _parsedErrors),
                        new JProperty("messages", _receivedMessages)
                        )));

            return json;
        }

        public UdpInputListener(CancellationToken cancelToken, int port = 5140)
            : base(cancelToken, "Win32-Udp")
        {
            _port = port;

            var groupV4 = new IPEndPoint(IPAddress.Any, 0);
            var groupV6 = new IPEndPoint(IPAddress.IPv6Any, 0);

            LogManager.GetCurrentClassLogger().Info("Udp Input on Port {0} Ready", _port);

            _receivedMessages = 0;

            _udpListener = new UdpClient(port);

            _listenThreadV4 = new Thread(StartListener);
            _listenThreadV4.Start(new ListenProfile { EndPoint = groupV4, Client = _udpListener });

            _listenThreadV6 = new Thread(StartListener);
            _listenThreadV6.Start(new ListenProfile { EndPoint = groupV6, Client = _udpListener });
        }

        public override void Shutdown()
        {
            LogManager.GetCurrentClassLogger().Info("Shutting Down {0}", InputType);
            _udpListener.Close();
            _listenThreadV4.Join(TimeSpan.FromSeconds(1));
            _listenThreadV6.Join(TimeSpan.FromSeconds(1));
            Finished();
            base.Shutdown();
        }

        private void StartListener(object useProfile)
        {
            var profile = (ListenProfile)useProfile;
            string lastMessage = "";
            try
            {
                while (!CancelToken.IsCancellationRequested)
                {
                    try
                    {
                        byte[] bytes = profile.Client.Receive(ref profile.EndPoint);  
                        var data = Encoding.UTF8.GetString(bytes, 0, bytes.Length);
                        lastMessage = data;
                        JObject json = JObject.Parse(data);
                        ProcessJson(json);
                        _receivedMessages++;
                    }
                    catch (Exception ex)
                    {
                        LogManager.GetCurrentClassLogger().Warn("Bad JSON: {0}", lastMessage);
                        LogManager.GetCurrentClassLogger().Warn(ex);
                        _parsedErrors++;
                    }
                }
                _udpListener.Close();
            }
            catch (Exception ex)
            {
                if (!CancelToken.IsCancellationRequested)
                {
                    LogManager.GetCurrentClassLogger().Error(ex);
                }
            }

            Finished();
        }
    }
}

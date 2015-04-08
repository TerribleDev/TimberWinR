using System;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
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
        private UdpClient _udpListenerV6;      
        private readonly Thread _listenThreadV6;

        private readonly int _port;
        private long _receivedMessages;
        private long _parsedErrors;
       
        public override JObject ToJson()
        {
            JObject json = new JObject(
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
     
            LogManager.GetCurrentClassLogger().Info("Udp Input on Port {0} Ready", _port);

            _receivedMessages = 0;

            _listenThreadV6 = new Thread(StartListener);
            _listenThreadV6.Start();
        }


        public override void Shutdown()
        {
            LogManager.GetCurrentClassLogger().Info("Shutting Down {0}", InputType);

            // close UDP listeners, which will end the listener threads          
            _udpListenerV6.Close();

            // wait for completion of the threads         
            _listenThreadV6.Join();

            Finished();

            base.Shutdown();
        }

        private void StartListener()
        {                      
            var groupV6 = new IPEndPoint(IPAddress.IPv6Any, _port);
            // Create the socket as IPv6
            var dualModeSocket = new Socket(AddressFamily.InterNetworkV6, SocketType.Dgram, ProtocolType.Udp);
            
            //
            // Now, disable the IPV6only flag to make it compatable with both ipv4 and ipv6
            // See: http://blogs.msdn.com/b/malarch/archive/2005/11/18/494769.aspx
            //
            dualModeSocket.SetSocketOption(SocketOptionLevel.IPv6, SocketOptionName.IPv6Only, 0);
            dualModeSocket.Bind(groupV6);

            _udpListenerV6 = new UdpClient();
            _udpListenerV6.Client = dualModeSocket;           

            string lastMessage = "";
            try
            {
                while (!CancelToken.IsCancellationRequested)
                {
                    try
                    {
                        byte[] bytes = _udpListenerV6.Receive(ref groupV6);  
                        var data = Encoding.UTF8.GetString(bytes, 0, bytes.Length);                                                
                        lastMessage = data;
                        var json = JObject.Parse(data);
                        ProcessJson(json);
                        Interlocked.Increment(ref _receivedMessages);                       
                    }
                    catch(SocketException)
                    {                       
                        break;
                    }
                    catch (Exception ex)
                    {
                        LogManager.GetCurrentClassLogger().Warn("Bad JSON: {0}", lastMessage);
                        LogManager.GetCurrentClassLogger().Warn(ex);
                        Interlocked.Increment(ref _parsedErrors);
                    }
                }
                _udpListenerV6.Close();
            }
            catch (Exception ex)
            {
                if (!CancelToken.IsCancellationRequested)
                    LogManager.GetCurrentClassLogger().Error(ex);
            }

            Finished();
        }
    }
}

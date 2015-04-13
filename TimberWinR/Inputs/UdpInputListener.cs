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
        private UdpClient _udpListenerV4;      
        private readonly Thread _listenThreadV4;

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
            _receivedMessages = 0;

            _listenThreadV4 = new Thread(StartListener);
            _listenThreadV4.Start();
        }


        public override void Shutdown()
        {
            LogManager.GetCurrentClassLogger().Info("Shutting Down {0}", InputType);

            // close UDP listeners, which will end the listener threads          
            _udpListenerV4.Close();

            // wait for completion of the threads         
            _listenThreadV4.Join();          

            base.Shutdown();
        }

        private void StartListener()
        {                      
            var groupV4 = new IPEndPoint(IPAddress.Any, _port);

            _udpListenerV4 = new UdpClient(_port);            
    
            LogManager.GetCurrentClassLogger().Info("Udp Input on Port {0} Ready", groupV4);

            string lastMessage = "";
            try
            {
                while (!CancelToken.IsCancellationRequested)
                {
                    try
                    {
                        byte[] bytes = _udpListenerV4.Receive(ref groupV4);  
                        var data = Encoding.UTF8.GetString(bytes, 0, bytes.Length);                                                
                        lastMessage = data;
                        var json = JObject.Parse(data);
                        ProcessJson(json);
                        Interlocked.Increment(ref _receivedMessages);                       
                    }
                    catch(ArgumentException aex)
                    {
                        LogManager.GetCurrentClassLogger().Error(aex);
                        break;
                    }
                    catch(SocketException)
                    {                       
                        break;
                    }
                    catch (Exception ex)
                    {                      
                        var jex1 = LogErrors.LogException(string.Format("Invalid JSON: {0}", lastMessage), ex);
                        if (jex1 != null)
                            ProcessJson(jex1);
      
                        LogManager.GetCurrentClassLogger().Warn("Bad JSON: {0}", lastMessage);
                        LogManager.GetCurrentClassLogger().Warn(ex);

                        Interlocked.Increment(ref _parsedErrors);
                    }
                }
                _udpListenerV4.Close();
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

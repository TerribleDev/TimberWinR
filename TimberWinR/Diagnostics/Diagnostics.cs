using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Net;
using System.Net.Sockets;
using System.IO;

using Newtonsoft.Json.Linq;

using NLog;
using TimberWinR.Parser;

namespace TimberWinR.Diagnostics
{
    public class Diagnostics
    {
        private CancellationToken CancelToken { get; set; }
        public int Port { get; set; }
        public Manager Manager { get; set; }

        private readonly System.Net.Sockets.TcpListener _tcpListenerV4;
        private readonly System.Net.Sockets.TcpListener _tcpListenerV6;
        private Thread _listenThreadV4;
        private Thread _listenThreadV6;

        public Diagnostics(Manager manager, CancellationToken cancelToken, int port = 5141)
        {
            Port = port;
            CancelToken = cancelToken;
            Manager = manager;

            LogManager.GetCurrentClassLogger().Info("Diagnostic(v4/v6) on Port {0} Ready", Port);

            _tcpListenerV6 = new System.Net.Sockets.TcpListener(IPAddress.IPv6Any, Port);
            _tcpListenerV4 = new System.Net.Sockets.TcpListener(IPAddress.Any, Port);

            _listenThreadV4 = new Thread(new ParameterizedThreadStart(ListenForClients));
            _listenThreadV4.Start(_tcpListenerV4);

            _listenThreadV6 = new Thread(new ParameterizedThreadStart(ListenForClients));
            _listenThreadV6.Start(_tcpListenerV6);

        }

        private void ListenForClients(object olistener)
        {
            var listener = olistener as System.Net.Sockets.TcpListener;

            listener.Start();
          
            while (!CancelToken.IsCancellationRequested)
            {
                try
                {
                    //blocks until a client has connected to the server
                    TcpClient client = listener.AcceptTcpClient();

                    // Wait for a client, spin up a thread.
                    var clientThread = new Thread(new ParameterizedThreadStart(HandleNewClient));
                    clientThread.Start(client);
                }
                catch (SocketException ex)
                {
                    if (ex.SocketErrorCode == SocketError.Interrupted)
                        break;
                    else
                        LogManager.GetCurrentClassLogger().Error(ex);
                }
            }
        }

        private void HandleNewClient(object client)
        {
            var tcpClient = (TcpClient)client;
            NetworkStream clientStream = null;

            try
            {
                using (clientStream = tcpClient.GetStream())
                {
                    StreamWriter sw = new StreamWriter(clientStream);
                    JObject json = new JObject(
                        new JProperty("timberwinr",
                            new JObject(
                                new JProperty("messages", Manager.NumMessages),
                                new JProperty("startedon", Manager.StartedOn),
                                new JProperty("configfile", Manager.JsonConfig),
                                new JProperty("logdir", Manager.LogfileDir),                              
                                new JProperty("logginglevel", LogManager.GlobalThreshold.ToString()),
                                new JProperty("inputs",
                                    new JArray(
                                        from i in Manager.Listeners                                    
                                        select new JObject(i.ToJson()))),             
                                new JProperty("outputs",
                                    new JArray(
                                        from o in Manager.Outputs
                                        select new JObject(o.ToJson()))))));
                                                                                 

                    sw.WriteLine(json.ToString());
                    sw.Flush();
                    tcpClient.Close();
                }
            }
            catch (Exception ex)
            {
                LogManager.GetCurrentClassLogger().Error("Tcp Exception", ex);
            }

        }

        public void Shutdown()
        {
            _tcpListenerV4.Stop();
            _tcpListenerV6.Stop();
        }

    }
}

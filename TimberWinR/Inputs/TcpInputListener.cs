using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Net;
using System.Net.Sockets;
using Newtonsoft.Json.Linq;
using NLog;

namespace TimberWinR.Inputs
{
    public class TcpInputListener : InputListener
    {
        private readonly System.Net.Sockets.TcpListener _tcpListener;
        private Thread _listenThread;       
        private readonly int _port;

        public TcpInputListener(CancellationToken cancelToken, int port = 5140)
            : base(cancelToken, "Win32-Tcp")
        {
            _port = port;
            _tcpListener = new System.Net.Sockets.TcpListener(IPAddress.Any, port);
            _listenThread = new Thread(new ThreadStart(ListenForClients));
            _listenThread.Start();
        }


        public override void Shutdown()
        {
            this._tcpListener.Stop();
            Finished();
            base.Shutdown();            
        }           
        

        private void ListenForClients()
        {
            this._tcpListener.Start();

            LogManager.GetCurrentClassLogger().Info("Tcp Input on Port {0} Ready", _port);

            while (!CancelToken.IsCancellationRequested)
            {
                try
                {
                    //blocks until a client has connected to the server
                    TcpClient client = this._tcpListener.AcceptTcpClient();                   
                  
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
            NetworkStream clientStream = tcpClient.GetStream();
            var stream = new StreamReader(clientStream);          
           
            string line;
            while ((line = stream.ReadLine()) != null)
            {
                try
                {
                    JObject json = JObject.Parse(line);
                    ProcessJson(json);
                }
                catch (Exception)
                {
                }
                if (CancelToken.IsCancellationRequested)
                    break;
            }
            clientStream.Close(); 
            tcpClient.Close();          
            Finished();
        }
    }
}

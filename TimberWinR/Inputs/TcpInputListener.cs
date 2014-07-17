using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Net;
using System.Net.Sockets;
using NLog;

namespace TimberWinR.Inputs
{
    public class TcpInputListener : InputListener
    {
        private readonly System.Net.Sockets.TcpListener _tcpListener;
        private Thread _listenThread;
        const int bufferSize = 16535;

        public TcpInputListener(CancellationToken cancelToken, int port = 5140) : base(cancelToken)
        {            
            _tcpListener = new System.Net.Sockets.TcpListener(IPAddress.Any, port);
            _listenThread = new Thread(new ThreadStart(ListenForClients));
            _listenThread.Start();
        }      

        public void Shutdown()
        {
            this._tcpListener.Stop();           
        }

        private void ListenForClients()
        {
            this._tcpListener.Start();

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

            var message = new byte[bufferSize];           
            while (!CancelToken.IsCancellationRequested)
            {
                var bytesRead = 0;
                try
                {
                    //blocks until a client sends a message                  
                    bytesRead = clientStream.Read(message, 0, bufferSize);
                }
                catch
                {
                    //a socket error has occured
                    break;
                }

                if (bytesRead == 0)
                {
                    //the client has disconnected from the server
                    break;
                }

                //message has successfully been received
                var encoder = new ASCIIEncoding();
                var encodedMessage = encoder.GetString(message, 0, bytesRead);

                ProcessJson(encodedMessage);             
            }
            tcpClient.Close();
        }
    }
}

using System;
using System.IO;
using System.Threading;
using System.Net;
using System.Net.Sockets;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using TimberWinR.Parser;
using System.Text;
using System.Collections.Generic;

namespace TimberWinR.UnitTests
{   
    // Class which implements a Fake redis server for test purposes.
    class FakeRediServer
    {
        private readonly System.Net.Sockets.TcpListener _tcpListenerV4;
        private readonly System.Net.Sockets.TcpListener _tcpListenerV6;
        private Thread _listenThreadV4;
        private Thread _listenThreadV6;
        private readonly int _port;
        private CancellationToken _cancelToken;
        private bool _shutdown;     

        public FakeRediServer(CancellationToken cancelToken, int port = 6379)
        {
            _port = port;
            _cancelToken = cancelToken;
            _shutdown = false;

            _tcpListenerV6 = new System.Net.Sockets.TcpListener(IPAddress.IPv6Any, port);
            _tcpListenerV4 = new System.Net.Sockets.TcpListener(IPAddress.Any, port);

            _listenThreadV4 = new Thread(new ParameterizedThreadStart(ListenForClients));
            _listenThreadV4.Start(_tcpListenerV4);

            _listenThreadV6 = new Thread(new ParameterizedThreadStart(ListenForClients));
            _listenThreadV6.Start(_tcpListenerV6);
        }

        public void Shutdown()
        {
            _shutdown = true;
            this._tcpListenerV4.Stop();
            this._tcpListenerV6.Stop();
        }


        private void ListenForClients(object olistener)
        {
            System.Net.Sockets.TcpListener listener = olistener as System.Net.Sockets.TcpListener;

            listener.Start();


            while (!_cancelToken.IsCancellationRequested && !_shutdown)
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
                }
            }
        }

        private void HandleNewClient(object client)
        {
            var tcpClient = (TcpClient)client;

            try
            {
                NetworkStream clientStream = tcpClient.GetStream();
                int i;
                Byte[] bytes = new Byte[16535];
                String data = null;

                do
                {
                    try
                    {
                        // Loop to receive all the data sent by the client.
                        while ((i = clientStream.Read(bytes, 0, bytes.Length)) != 0)
                        {
                            // Translate data bytes to a ASCII string.
                            data = System.Text.Encoding.ASCII.GetString(bytes, 0, i);
                            //System.Diagnostics.Debug.WriteLine(String.Format("Received: {0}", data));

                            // Process the data sent by the client.
                            data = ":1000\r\n";

                            byte[] msg = System.Text.Encoding.ASCII.GetBytes(data);

                            // Send back a response.
                            clientStream.Write(msg, 0, msg.Length);
                          //  System.Diagnostics.Debug.WriteLine(String.Format("Sent: {0}", data));
                        }
                    }
                    catch (IOException)
                    {                       
                    }
                } while (true);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex.ToString());
            }
            tcpClient.Close();
        }

        private void ProcessJson(JObject json)
        {
            Console.WriteLine(json.ToString());
        }

    }
}

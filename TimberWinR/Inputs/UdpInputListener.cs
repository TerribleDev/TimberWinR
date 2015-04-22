using System;
using System.Collections.Concurrent;
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
        private UdpClient _udpListenerV4;
        private IPEndPoint _udpEndpointV4;
        private readonly BlockingCollection<byte[]> _unprocessedRawData;
        private readonly Thread _rawDataProcessingThread;
        private readonly int _port;
        private long _receivedMessages;
        private long _parseErrors;
        private long _receiveErrors;
        private long _parsedMessages;

        public override JObject ToJson()
        {
            var json =
                new JObject(new JProperty("udp",
                    new JObject(new JProperty("port", _port),
                        new JProperty("receive_errors", _receiveErrors),
                        new JProperty("parse_errors", _parseErrors),
                        new JProperty("messages", _receivedMessages),
                        new JProperty("parsed_messages", _parsedMessages),
                        new JProperty("unprocessed_messages", _unprocessedRawData.Count))));

            return json;
        }

        public UdpInputListener(CancellationToken cancelToken, int port = 5140) : base(cancelToken, "Win32-Udp")
        {
            _port = port;
            _receivedMessages = 0;

            // setup raw data processor
            _unprocessedRawData = new BlockingCollection<byte[]>();
            _rawDataProcessingThread = new Thread(ProcessDataLoop) { Name = "Win32-Udp-DataProcessor"};
            _rawDataProcessingThread.Start();

            // start listing to udp port
            StartListener();
        }

        public override void Shutdown()
        {
            LogManager.GetCurrentClassLogger().Info("Shutting Down {0}", InputType);

            // close UDP listeners, which will end the listener threads          
            _udpListenerV4.Close();

            base.Shutdown();
        }

        private void StartListener()
        {
            _udpEndpointV4 = new IPEndPoint(IPAddress.Any, _port);

            // setup listener
            _udpListenerV4 = new UdpClient(_port);

            // start listening on UDP port
            StartReceiving();

            // all started; log details
            LogManager.GetCurrentClassLogger().Info("Udp Input on Port {0} Ready", _udpEndpointV4);
        }

        private void StartReceiving()
        {
            if (!CancelToken.IsCancellationRequested)
                _udpListenerV4.BeginReceive(DataReceived, null);
        }

        private void DataReceived(IAsyncResult result)
        {
            if (CancelToken.IsCancellationRequested)
            {
                _unprocessedRawData.CompleteAdding();
                return;
            }

            try
            {
                byte[] bytes = _udpListenerV4.EndReceive(result, ref _udpEndpointV4);
                Interlocked.Increment(ref _receivedMessages);
                StartReceiving();
                _unprocessedRawData.Add(bytes);
            }
            catch (SocketException)
            {
                LogManager.GetCurrentClassLogger().Info("Socked exception. Ending UDP Listener.");
                _unprocessedRawData.CompleteAdding();
            }
            catch (ObjectDisposedException)
            {
                LogManager.GetCurrentClassLogger().Info("Object disposed. Ending UDP Listener");
                _unprocessedRawData.CompleteAdding();
            }
            catch (Exception ex)
            {
                LogManager.GetCurrentClassLogger().Warn("Error while receiving data.", ex);

                Interlocked.Increment(ref _receiveErrors);
                StartReceiving();
            }
        }

        private void ProcessDataLoop()
        {           
            while (!_unprocessedRawData.IsCompleted)
            {
                try
                {
                    ProcessData(_unprocessedRawData.Take());
                }
                catch (OperationCanceledException)
                {
                    // we are shutting down.
                    break;
                }
                catch (InvalidOperationException)
                {
                    // when the collection is marked as completed
                    break;
                }
                catch (Exception ex)
                {
                    LogManager.GetCurrentClassLogger().ErrorException("Error while processing data", ex);
                    Thread.Sleep(100);
                }
            }

            Finished();
        }

        private void ProcessData(byte[] bytes)
        {
            var data = Encoding.UTF8.GetString(bytes, 0, bytes.Length);

            try
            {
                var json = JObject.Parse(data);
                ProcessJson(json);

                _parsedMessages++;
            }
            catch (Exception ex)
            {
                var jex1 = LogErrors.LogException(string.Format("Invalid JSON: {0}", data), ex);
                if (jex1 != null)
                {
                    ProcessJson(jex1);
                }

                var msg = string.Format("Bad JSON: {0}", data);
                LogManager.GetCurrentClassLogger().Warn(msg, ex);

                _parseErrors++;
            }
        }
    }
}

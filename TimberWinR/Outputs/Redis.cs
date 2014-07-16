using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using ctstone.Redis;
using NLog;
using System.Threading.Tasks;

namespace TimberWinR.Outputs
{
    public class RedisOutput : OutputSender
    {       
        private readonly string _logstashIndexName;
        private readonly string _hostname;
        private readonly int _port;
        private readonly int _timeout;
        private object _locker = new object();
        private List<string> _jsonQueue;
        readonly Task _consumerTask;
        private string[] _redisHosts;
        private int _redisHostIndex;

        /// <summary>
        /// Get the next client
        /// </summary>
        /// <returns></returns>
        private RedisClient getClient()
        {
            if (_redisHostIndex >= _redisHosts.Length)
                _redisHostIndex = 0;
                        
            int numTries = 0;
            while (numTries < _redisHosts.Length)
            {
                try
                {
                    RedisClient client = new RedisClient(_redisHosts[_redisHostIndex], _port, _timeout);
                    _redisHostIndex++;
                    if (_redisHostIndex >= _redisHosts.Length)
                        _redisHostIndex = 0;
                  
                    return client;
                }
                catch (Exception)
                {
                }
                numTries++;
            }         

            return null;
        }

        public RedisOutput(string[] redisHosts, CancellationToken cancelToken, string logstashIndexName = "logstash", int port = 6379, int timeout = 10000)
            : base(cancelToken)
        {
            _redisHostIndex = 0;
            _redisHosts = redisHosts;
            _jsonQueue = new List<string>();          
            _port = port;
            _timeout = timeout;
            _logstashIndexName = logstashIndexName;          
            _consumerTask = new Task(RedisSender, CancellationToken.None);
            _consumerTask.Start();
        }

        
        /// <summary>
        /// Forward on Json message to Redis Logstash queue
        /// </summary>
        /// <param name="jsonMessage"></param>
        protected override void MessageReceivedHandler(string jsonMessage)
        {
            LogManager.GetCurrentClassLogger().Info(jsonMessage);

            lock (_locker)
            {
                _jsonQueue.Add(jsonMessage);
            }
        }

        // 
        // Pull off messages from the Queue, batch them up and send them all across
        // 
        private void RedisSender()
        {
            while (!CancelToken.IsCancellationRequested)
            {
                string[] messages;
                lock (_locker)
                {
                    messages = _jsonQueue.ToArray();
                    _jsonQueue.Clear();
                }

                if (messages.Length > 0)
                {
                    int numHosts = _redisHosts.Length;
                    while (numHosts-- > 0)
                    {
                        try
                        {
                            // Get the next client
                            using (RedisClient client = getClient())
                            {
                                if (client != null)
                                {
                                    client.StartPipe();

                                    foreach (string jsonMessage in messages)
                                    {
                                        try
                                        {
                                            client.RPush(_logstashIndexName, jsonMessage);
                                        }
                                        catch (SocketException)
                                        {
                                        }
                                    }
                                    client.EndPipe();                                    
                                    break;
                                }
                                else
                                {
                                    LogManager.GetCurrentClassLogger()
                                        .Fatal("Unable to connect with any Redis hosts, {0}",
                                            String.Join(",", _redisHosts));
                                }
                            }
                        }
                        catch(Exception)
                        {
                            // Got an error, try the other hosts                         
                        }
                    }
                }
                System.Threading.Thread.Sleep(1000);
            }
        }
    }
}

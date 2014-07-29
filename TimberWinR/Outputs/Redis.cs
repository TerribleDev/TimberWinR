using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using ctstone.Redis;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NLog;
using System.Threading.Tasks;
using RapidRegex.Core;
using System.Text.RegularExpressions;
using System.Globalization;

namespace TimberWinR.Outputs
{
    public class RedisOutput : OutputSender
    {
        private readonly string _logstashIndexName;
        private readonly int _port;
        private readonly int _timeout;
        private readonly object _locker = new object();
        private readonly List<string> _jsonQueue;
       // readonly Task _consumerTask;
        private readonly string[] _redisHosts;
        private int _redisHostIndex;
        private TimberWinR.Manager _manager;
        private readonly int _batchCount;
        private readonly int _interval;

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
                catch (Exception )
                {
                }
                numTries++;
            }

            return null;
        }

        public RedisOutput(TimberWinR.Manager manager, Parser.RedisOutput ro, CancellationToken cancelToken) //string[] redisHosts, string logstashIndexName = "logstash", int port = 6379, int timeout = 10000, int batch_count = 10)
            : base(cancelToken)
        {
            _batchCount = ro.BatchCount;
            _manager = manager;
            _redisHostIndex = 0;
            _redisHosts = ro.Host;
            _jsonQueue = new List<string>();
            _port = ro.Port;
            _timeout = ro.Timeout;
            _logstashIndexName = ro.Index;
            _interval = ro.Interval;

            for (int i = 0; i < ro.NumThreads; i++)
            {
                var redisThread = new Task(RedisSender, cancelToken);
                redisThread.Start();
            }
        }


        /// <summary>
        /// Forward on Json message to Redis Logstash queue
        /// </summary>
        /// <param name="jsonMessage"></param>
        protected override void MessageReceivedHandler(JObject jsonMessage)
        {
            if (_manager.Config.Filters != null)
                ApplyFilters(jsonMessage);

            var message = jsonMessage.ToString();
            LogManager.GetCurrentClassLogger().Trace(message);

            lock (_locker)
            {
                _jsonQueue.Add(message);
            }
        }

        private void ApplyFilters(JObject json)
        {
            foreach (var filter in _manager.Config.Filters)
            {
                filter.Apply(json);
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
                    messages = _jsonQueue.Take(_batchCount).ToArray();
                    _jsonQueue.RemoveRange(0, messages.Length);                   
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
                                    LogManager.GetCurrentClassLogger()
                                               .Info("Sending {0} Messages to {1}", messages.Length, client.Host);

                                    foreach (string jsonMessage in messages)
                                    {
                                        try
                                        {                                           
                                            client.RPush(_logstashIndexName, jsonMessage);
                                        }
                                        catch (SocketException ex)
                                        {
                                            LogManager.GetCurrentClassLogger().Warn(ex);
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
                        catch (Exception ex)
                        {
                            LogManager.GetCurrentClassLogger().Error(ex);
                        }
                    }
                }
                System.Threading.Thread.Sleep(_interval);
            }
        }
    }
}

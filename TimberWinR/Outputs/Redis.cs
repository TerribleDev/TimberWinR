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
        private readonly int _numThreads;

        private long _sentMessages;
        private long _errorCount;
        private long _redisDepth;

        private int _maxQueueSize;
        private bool _queueOverflowDiscardOldest;

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

        public override JObject ToJson()
        {
            JObject json = new JObject(
                new JProperty("redis",
                    new JObject(
                        new JProperty("host", string.Join(",", _redisHosts)),
                        new JProperty("errors", _errorCount),
                        new JProperty("redis_depth", _redisDepth),
                        new JProperty("sent_messages", _sentMessages),
                        new JProperty("queued_messages", _jsonQueue.Count),
                        new JProperty("port", _port),
                        new JProperty("interval", _interval),
                        new JProperty("threads", _numThreads),
                        new JProperty("batchcount", _batchCount),
                        new JProperty("index", _logstashIndexName),
                        new JProperty("hosts",
                            new JArray(
                                from h in _redisHosts
                                select new JObject(
                                    new JProperty("host", h)))))));
            return json;
        }

        public RedisOutput(TimberWinR.Manager manager, Parser.RedisOutput ro, CancellationToken cancelToken)
            : base(cancelToken, "Redis")
        {
            _redisDepth = 0;
            _batchCount = ro.BatchCount;
            _manager = manager;
            _redisHostIndex = 0;
            _redisHosts = ro.Host;
            _jsonQueue = new List<string>();
            _port = ro.Port;
            _timeout = ro.Timeout;
            _logstashIndexName = ro.Index;
            _interval = ro.Interval;
            _numThreads = ro.NumThreads;
            _errorCount = 0;
            _maxQueueSize = ro.MaxQueueSize;
            _queueOverflowDiscardOldest = ro.QueueOverflowDiscardOldest;

            for (int i = 0; i < ro.NumThreads; i++)
            {
                var redisThread = new Task(RedisSender, cancelToken);
                redisThread.Start();
            }
        }

        public override string ToString()
        {
            return string.Format("Redis Host: {0} Port: {1}, Threads: {2}, Interval: {3}, BatchCount: {4}", string.Join(",", _redisHosts) , _port, _numThreads, _interval, _batchCount);
        }

        /// <summary>
        /// Forward on Json message to Redis Logstash queue
        /// </summary>
        /// <param name="jsonMessage"></param>
        protected override void MessageReceivedHandler(JObject jsonMessage)
        {
            if (_manager.Config.Filters != null)
            {
                if (ApplyFilters(jsonMessage))
                    return;
            }

            var message = jsonMessage.ToString();
            LogManager.GetCurrentClassLogger().Debug(message);

            lock (_locker)
            {
                if (_jsonQueue.Count >= _maxQueueSize)
                {
                    // If we've exceeded our queue size, and we're supposed to throw out the oldest objects first,
                    // then remove as many as necessary to get us under our limit
                    if (_queueOverflowDiscardOldest)
                    {
                        for (int i = 0; i <= (_jsonQueue.Count - _maxQueueSize); i++)
                        {
                            _jsonQueue.RemoveAt(0);
                        }
                    }
                    // Otherwise we're in a "discard newest" mode, and this is the newest message, so just ignore it
                    else
                    {
                        return;
                    }
                }

                _jsonQueue.Add(message);
            }
        }

        private bool ApplyFilters(JObject json)
        {
            bool drop = false;
            foreach (var filter in _manager.Config.Filters)
            {
                if (!filter.Apply(json))
                {
                    LogManager.GetCurrentClassLogger().Debug("Dropping: {0}", json.ToString());
                    drop = true;
                }
            }
            return drop;
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
                    if (messages.Length > 0)
                        _manager.IncrementMessageCount(messages.Length);
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
                                               .Debug("Sending {0} Messages to {1}", messages.Length, client.Host);

                                    try
                                    {
                                        _redisDepth = client.RPush(_logstashIndexName, messages);
                                        _sentMessages += messages.Length;
                                    }
                                    catch (SocketException ex)
                                    {
                                        LogManager.GetCurrentClassLogger().Warn(ex);
                                        Interlocked.Increment(ref _errorCount);
                                    }
                                    finally
                                    {
                                        client.EndPipe();
                                    }
                                    break;
                                }
                                else
                                {
                                    Interlocked.Increment(ref _errorCount);
                                    LogManager.GetCurrentClassLogger()
                                        .Fatal("Unable to connect with any Redis hosts, {0}",
                                            String.Join(",", _redisHosts));
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            LogManager.GetCurrentClassLogger().Error(ex);
                            Interlocked.Increment(ref _errorCount);
                        }
                    }
                }
                GC.Collect();
                System.Threading.Thread.Sleep(_interval);
            }
        }
    }
}

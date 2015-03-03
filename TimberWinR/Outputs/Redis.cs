using System;
using System.Collections.Generic;
using System.Diagnostics.Eventing.Reader;
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
using TimberWinR.Parser;

namespace TimberWinR.Outputs
{
    public class RedisOutput : OutputSender
    {
        private readonly string _logstashIndexName;
        private readonly int _port;
        private readonly int _timeout;
        private readonly object _locker = new object();
        private readonly List<string> _jsonQueue;       
        private readonly string[] _redisHosts;
        private int _redisHostIndex;
        private TimberWinR.Manager _manager;
        private readonly int _batchCount;
        private int _currentBatchCount;
        private readonly int _maxBatchCount;       
        private readonly int _interval;
        private readonly int _numThreads;
        private readonly int[] _sampleQueueDepths;
        private int _sampleCountIndex;
        private long _sentMessages;
        private long _errorCount;
        private long _redisDepth;
        private DateTime? _lastErrorTime;
        private const int QUEUE_SAMPLE_SIZE = 30; // 30 samples over 2.5 minutes (default)
        private readonly int _maxQueueSize;
        private readonly bool _queueOverflowDiscardOldest;
        private bool _warnedReachedMax;

        public bool Stop { get; set; }

        /// <summary>
        /// Get the next client from the list of hosts.
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
                    return client;
                }
                catch (Exception)
                {
                }
                finally
                {
                    _redisHostIndex++;
                    if (_redisHostIndex >= _redisHosts.Length)
                        _redisHostIndex = 0;
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
                        new JProperty("lastErrorTime", _lastErrorTime),
                        new JProperty("redis_depth", _redisDepth),
                        new JProperty("sent_messages", _sentMessages),
                        new JProperty("queued_messages", _jsonQueue.Count),
                        new JProperty("port", _port),
                        new JProperty("maxQueueSize", _maxQueueSize),
                        new JProperty("overflowDiscardOldest", _queueOverflowDiscardOldest),
                        new JProperty("interval", _interval),
                        new JProperty("threads", _numThreads),
                        new JProperty("batchcount", _batchCount),
                        new JProperty("currentBatchCount", _currentBatchCount),
                        new JProperty("maxBatchCount", _maxBatchCount),
                        new JProperty("averageQueueDepth", AverageQueueDepth()),   
                        new JProperty("queueSamples", new JArray(_sampleQueueDepths)),
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
            _warnedReachedMax = false;
            // Last QUEUE_SAMPLE_SIZE queue samples (spans timestamp * 10)
            _sampleQueueDepths = new int[QUEUE_SAMPLE_SIZE];
            _sampleCountIndex = 0;
            _redisDepth = 0;
            _batchCount = ro.BatchCount;
            _maxBatchCount = ro.MaxBatchCount;
            // Make sure maxBatchCount is larger than batchCount
            if (_maxBatchCount < _batchCount)
                _maxBatchCount = _batchCount*10;
            _currentBatchCount = _batchCount;
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
            _lastErrorTime = null;
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
            return string.Format("Redis Host: {0} Port: {1}, Threads: {2}, Interval: {3}, BatchCount: {4}", string.Join(",", _redisHosts), _port, _numThreads, _interval, _batchCount);
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
                        LogManager.GetCurrentClassLogger()
                            .Warn("Overflow discarding oldest {0} messages", _jsonQueue.Count - _maxQueueSize + 1);

                        _jsonQueue.RemoveRange(0, (_jsonQueue.Count - _maxQueueSize) + 1);
                    }
                    // Otherwise we're in a "discard newest" mode, and this is the newest message, so just ignore it
                    else
                    {
                        LogManager.GetCurrentClassLogger()
                            .Warn("Overflow discarding newest message: {0}", message);

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
        // What is average queue depth?
        //
        private int AverageQueueDepth()
        {
            lock(_locker)
            {
                return (int)_sampleQueueDepths.Average();
            }
        }

        // 
        // Pull off messages from the Queue, batch them up and send them all across
        // 
        private void RedisSender()
        {
            using (var syncHandle = new ManualResetEventSlim())
            {
                // Execute the query
                while (!Stop)
                {
                    if (!CancelToken.IsCancellationRequested)
                    {
                        try
                        {
                            string[] messages;
                            // Exclusively
                            lock (_locker)
                            {           
                                // Take a sample of the queue depth
                                if (_sampleCountIndex >= QUEUE_SAMPLE_SIZE)
                                    _sampleCountIndex = 0;     
                                _sampleQueueDepths[_sampleCountIndex++] = _jsonQueue.Count;                                                                                    
                                messages = _jsonQueue.Take(_currentBatchCount).ToArray();
                                _jsonQueue.RemoveRange(0, messages.Length);                               
                                // Re-compute current batch size
                                updateCurrentBatchCount();
                            }

                            if (messages.Length > 0)
                            {
                                int numHosts = _redisHosts.Length;
                                bool sentSuccessfully = false;
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
                                                    client.EndPipe();
                                                    sentSuccessfully = true;
                                                    if (messages.Length > 0)
                                                        _manager.IncrementMessageCount(messages.Length);
                                                }
                                                catch (SocketException ex)
                                                {
                                                    LogManager.GetCurrentClassLogger().Warn(ex);
                                                    Interlocked.Increment(ref _errorCount);
                                                    _lastErrorTime = DateTime.UtcNow;
                                                }
                                                catch (Exception ex)
                                                {
                                                    LogManager.GetCurrentClassLogger().Error(ex);
                                                    Interlocked.Increment(ref _errorCount);
                                                    _lastErrorTime = DateTime.UtcNow;
                                                }                                               
                                                break;
                                            }
                                            else
                                            {
                                                Interlocked.Increment(ref _errorCount);
                                                LogManager.GetCurrentClassLogger()
                                                    .Fatal("Unable to connect with any Redis hosts, {0}",
                                                        String.Join(",", _redisHosts));
                                                _lastErrorTime = DateTime.UtcNow;
                                            }
                                        }
                                    }
                                    catch (Exception ex)
                                    {
                                        LogManager.GetCurrentClassLogger().Error(ex);
                                        Interlocked.Increment(ref _errorCount);
                                        _lastErrorTime = DateTime.UtcNow;                                
                                    }
                                } // No more hosts to try.

 
                                if (!sentSuccessfully)                                
                                {
                                    lock (_locker)
                                    {
                                        _jsonQueue.InsertRange(0, messages);
                                    }
                                }
                            }
                            GC.Collect();
                            if (!Stop)
                                syncHandle.Wait(TimeSpan.FromMilliseconds(_interval), CancelToken);
                        }
                        catch (OperationCanceledException oce)
                        {
                            break;
                        }
                        catch (Exception ex)
                        {
                            _lastErrorTime = DateTime.UtcNow;
                            Interlocked.Increment(ref _errorCount);
                            LogManager.GetCurrentClassLogger().Error(ex);                         
                        }
                    }
                }
            }
        }

        // Sample the queue and adjust the batch count if needed (ramp up slowly)
        private void updateCurrentBatchCount()
        {
            if (_currentBatchCount < _maxBatchCount && AverageQueueDepth() > _currentBatchCount)
            {
                _currentBatchCount += _maxBatchCount/QUEUE_SAMPLE_SIZE;
                if (_currentBatchCount >= _maxBatchCount && !_warnedReachedMax)
                {
                    LogManager.GetCurrentClassLogger().Warn("Maximum Batch Count of {0} reached.", _currentBatchCount);
                    _warnedReachedMax = true; // Only complain when it's reached (1 time, unless reset)
                }
            }
            else // Reset to default
            {
                _currentBatchCount = _batchCount;
                _warnedReachedMax = false;
            }
        }
    }
}

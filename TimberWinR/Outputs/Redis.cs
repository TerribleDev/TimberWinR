using System;
using System.Collections.Generic;
using System.Diagnostics.Eventing.Reader;
using System.Linq;
using System.Linq.Expressions;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using CSRedis;
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
    internal class BatchCounter
    {
        // Total number of times reached max batch count (indicates we are under pressure)
        public int ReachedMaxBatchCountTimes { get; set; }

        private readonly int[] _sampleQueueDepths;
        private int _sampleCountIndex;
        private const int QUEUE_SAMPLE_SIZE = 30; // 30 samples over 2.5 minutes (default)
        private object _locker = new object();
        private bool _warnedReachedMax;

        private readonly int _maxBatchCount;
        private readonly int _batchCount;
        private int _totalSamples;

        public int[] Samples()
        {
            return _sampleQueueDepths;
        }

        public BatchCounter(int batchCount, int maxBatchCount)
        {
            _batchCount = batchCount;
            _maxBatchCount = maxBatchCount;
            _sampleQueueDepths = new int[QUEUE_SAMPLE_SIZE];
            _sampleCountIndex = 0;
            _totalSamples = 0;
            ReachedMaxBatchCountTimes = 0;
        }
        public void SampleQueueDepth(int queueDepth)
        {
            lock (_locker)
            {
                if (_totalSamples < QUEUE_SAMPLE_SIZE)
                    _totalSamples++;

                // Take a sample of the queue depth
                if (_sampleCountIndex >= QUEUE_SAMPLE_SIZE)
                    _sampleCountIndex = 0;

                _sampleQueueDepths[_sampleCountIndex++] = queueDepth;
            }
        }

        public int AverageQueueDepth()
        {
            lock (_locker)
            {
                if (_totalSamples > 0)
                {
                    var samples = _sampleQueueDepths.Take(_totalSamples);
                    int avg = (int)samples.Average();
                    return avg;
                }
                return 0;
            }
        }

        // Sample the queue and adjust the batch count if needed (ramp up slowly)
        public int UpdateCurrentBatchCount(int queueSize, int currentBatchCount)
        {
            if (currentBatchCount < _maxBatchCount && currentBatchCount < queueSize && AverageQueueDepth() > currentBatchCount)
            {
                currentBatchCount += Math.Max(_maxBatchCount / _batchCount, 1);
                if (currentBatchCount >= _maxBatchCount && !_warnedReachedMax)
                {
                    LogManager.GetCurrentClassLogger().Warn("Maximum Batch Count of {0} reached.", currentBatchCount);
                    _warnedReachedMax = true; // Only complain when it's reached (1 time, unless reset)
                    ReachedMaxBatchCountTimes++;
                    currentBatchCount = _maxBatchCount;
                }
            }
            else // Reset to default
            {
                currentBatchCount = _batchCount;
                _warnedReachedMax = false;
            }

            return currentBatchCount;
        }
    }


    public class RedisOutput : OutputSender
    {
        public int QueueDepth
        {
            get { return _jsonQueue.Count; }
        }

        public long SentMessages
        {
            get { return _sentMessages; }
        }

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
        private long _sentMessages;
        private long _errorCount;
        private long _redisDepth;
        private DateTime? _lastErrorTimeUTC;
        private readonly int _maxQueueSize;
        private readonly bool _queueOverflowDiscardOldest;
        private BatchCounter _batchCounter;

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
                    RedisClient client = new RedisClient(_redisHosts[_redisHostIndex], _port);                   
                    return client;
                }
                catch (Exception ex)
                {
                    LogManager.GetCurrentClassLogger().Error(ex);

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
                        new JProperty("lastErrorTimeUTC", _lastErrorTimeUTC),
                        new JProperty("redisQueueDepth", _redisDepth),
                        new JProperty("sentMessageCount", _sentMessages),
                        new JProperty("queuedMessageCount", _jsonQueue.Count),
                        new JProperty("port", _port),
                        new JProperty("maxQueueSize", _maxQueueSize),
                        new JProperty("overflowDiscardOldest", _queueOverflowDiscardOldest),
                        new JProperty("interval", _interval),
                        new JProperty("threads", _numThreads),
                        new JProperty("batchcount", _batchCount),
                        new JProperty("currentBatchCount", _currentBatchCount),
                        new JProperty("reachedMaxBatchCountTimes", _batchCounter.ReachedMaxBatchCountTimes),
                        new JProperty("maxBatchCount", _maxBatchCount),
                        new JProperty("averageQueueDepth", _batchCounter.AverageQueueDepth()),
                        new JProperty("queueSamples", new JArray(_batchCounter.Samples())),
                        new JProperty("index", _logstashIndexName),
                        new JProperty("hosts",
                            new JArray(
                                from h in _redisHosts
                                select new JObject(
                                    new JProperty("host", h)))))));
            return json;
        }

        public RedisOutput(TimberWinR.Manager manager, Parser.RedisOutputParameters parameters, CancellationToken cancelToken)
            : base(cancelToken, "Redis")
        {
            _redisDepth = 0;
            _batchCount = parameters.BatchCount;
            _maxBatchCount = parameters.MaxBatchCount;
            // Make sure maxBatchCount is larger than batchCount
            if (_maxBatchCount <= _batchCount)
                _maxBatchCount = _batchCount * 10;

            _manager = manager;
            _redisHostIndex = 0;
            _redisHosts = parameters.Host;
            _jsonQueue = new List<string>();
            _port = parameters.Port;
            _timeout = parameters.Timeout;
            _logstashIndexName = parameters.Index;
            _interval = parameters.Interval;
            _numThreads = parameters.NumThreads;
            _errorCount = 0;
            _lastErrorTimeUTC = null;
            _maxQueueSize = parameters.MaxQueueSize;
            _queueOverflowDiscardOldest = parameters.QueueOverflowDiscardOldest;
            _batchCounter = new BatchCounter(_batchCount, _maxBatchCount);
            _currentBatchCount = _batchCount;

            for (int i = 0; i < parameters.NumThreads; i++)
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
            LogManager.GetCurrentClassLogger().Trace(message);

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
                    LogManager.GetCurrentClassLogger().Debug("{0}: Dropping: {1}", Thread.CurrentThread.ManagedThreadId, json.ToString());
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
                                _batchCounter.SampleQueueDepth(_jsonQueue.Count);
                                // Re-compute current batch size
                                _currentBatchCount = _batchCounter.UpdateCurrentBatchCount(_jsonQueue.Count, _currentBatchCount);

                                messages = _jsonQueue.Take(_currentBatchCount).ToArray();
                                _jsonQueue.RemoveRange(0, messages.Length);
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
                                                    .Debug("{0}: Sending {1} Messages to {2}", Thread.CurrentThread.ManagedThreadId, messages.Length, client.Host);

                                                try
                                                {
                                                    _redisDepth = client.RPush(_logstashIndexName, messages);
                                                    Interlocked.Add(ref _sentMessages, messages.Length);
                                                    client.EndPipe();
                                                    sentSuccessfully = true;
                                                    if (messages.Length > 0)
                                                        _manager.IncrementMessageCount(messages.Length);
                                                }
                                                catch (SocketException ex)
                                                {
                                                    LogManager.GetCurrentClassLogger().Warn(ex);
                                                    Interlocked.Increment(ref _errorCount);
                                                    _lastErrorTimeUTC = DateTime.UtcNow;
                                                }
                                                catch (Exception ex)
                                                {
                                                    LogManager.GetCurrentClassLogger().Error(ex);
                                                    Interlocked.Increment(ref _errorCount);
                                                    _lastErrorTimeUTC = DateTime.UtcNow;
                                                }
                                                break;
                                            }
                                            else
                                            {
                                                Interlocked.Increment(ref _errorCount);
                                                LogManager.GetCurrentClassLogger()
                                                    .Fatal("Unable to connect with any Redis hosts, {0}",
                                                        String.Join(",", _redisHosts));
                                                _lastErrorTimeUTC = DateTime.UtcNow;
                                            }
                                        }
                                    }
                                    catch (Exception ex)
                                    {
                                        LogManager.GetCurrentClassLogger().Error(ex);
                                        Interlocked.Increment(ref _errorCount);
                                        _lastErrorTimeUTC = DateTime.UtcNow;
                                    }
                                } // No more hosts to try.

                                // Couldn't send, put it back into the queue.
                                if (!sentSuccessfully)
                                {
                                    lock (_locker)
                                    {
                                        _jsonQueue.InsertRange(0, messages);
                                    }
                                }
                            }                          
                            if (!Stop)
                                syncHandle.Wait(TimeSpan.FromMilliseconds(_interval), CancelToken);
                        }
                        catch (OperationCanceledException)
                        {
                            break;
                        }
                        catch (ThreadAbortException)
                        {
                            break;
                        }
                        catch (Exception ex)
                        {
                            _lastErrorTimeUTC = DateTime.UtcNow;
                            Interlocked.Increment(ref _errorCount);
                            LogManager.GetCurrentClassLogger().Error(ex);
                        }
                    }
                }
            }
        }
    }
}

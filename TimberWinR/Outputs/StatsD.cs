using System;
using System.Collections.Generic;
using System.Diagnostics.Eventing.Reader;
using System.Linq;
using System.Linq.Expressions;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using CSRedis;
using Nest;
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
    public class StatsDOutput : OutputSender
    {
        public int QueueDepth
        {
            get { return _jsonQueue.Count; }
        }

        public long SentMessages
        {
            get { return _sentMessages; }
        }

       
        private readonly int _port;
        public string _host { get; set; }

        private readonly int _interval;
        private readonly object _locker = new object();
        private readonly List<JObject> _jsonQueue;
        private TimberWinR.Manager _manager;        
        private long _sentMessages;
        private long _errorCount;
        private readonly int _maxQueueSize;
        private readonly bool _queueOverflowDiscardOldest;
        private readonly int _flushSize;
        private readonly int _idleFlushTimeSeconds;
        private readonly int _numThreads;
        private Parser.StatsDOutputParameters _params;

        public bool Stop { get; set; }
       
        public override JObject ToJson()
        {
            var json = new JObject(
                new JProperty("statsd",
                    new JObject(                       
                        new JProperty("errors", _errorCount),                       
                        new JProperty("sentMessageCount", _sentMessages),
                        new JProperty("queuedMessageCount", _jsonQueue.Count),
                        new JProperty("port", _port),
                        new JProperty("threads", _numThreads),
                        new JProperty("flushSize", _flushSize),
                        new JProperty("idleFlushTime", _idleFlushTimeSeconds),  
                        new JProperty("maxQueueSize", _maxQueueSize),
                        new JProperty("overflowDiscardOldest", _queueOverflowDiscardOldest),
                        new JProperty("interval", _interval),
                        new JProperty("host", _host)
                        )));
                       
            return json;
        }

        public StatsDOutput(TimberWinR.Manager manager, Parser.StatsDOutputParameters parameters, CancellationToken cancelToken)
            : base(cancelToken, "StatsD")
        {
            _params = parameters;
            _manager = manager;                     
            _port = parameters.Port;
            _host = parameters.Host;
            _interval = parameters.Interval;
            _flushSize = parameters.FlushSize;
            _idleFlushTimeSeconds = parameters.IdleFlushTimeInSeconds;
            _maxQueueSize = parameters.MaxQueueSize;
            _queueOverflowDiscardOldest = parameters.QueueOverflowDiscardOldest;
            _numThreads = parameters.NumThreads;
            _jsonQueue = new List<JObject>();

            NStatsD.Client.Host = _host;
            NStatsD.Client.Port = _port;

            for (int i = 0; i < _numThreads; i++)
            {
                Task.Factory.StartNew(StatsDSender, cancelToken, TaskCreationOptions.LongRunning, TaskScheduler.Current);
            }

        }

        public override string ToString()
        {
            return string.Format("StatsD Host: {0} Port: {1}", _host, _port);
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

                _jsonQueue.Add(jsonMessage);
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
            // Check for matching type (if defined).
            if (!drop && !string.IsNullOrEmpty(_params.InputType) && json["type"] != null)
            {
                string msgType = json["type"].ToString();
                if (!string.IsNullOrEmpty(msgType) && msgType != _params.InputType)
                    return true;
            }

            return drop;
        }

        // Places messages back into the queue (for a future attempt)
        private void interlockedInsert(List<JObject> messages)
        {
            lock (_locker)
            {
                Interlocked.Increment(ref _errorCount);
                _jsonQueue.InsertRange(0, messages);
                if (_jsonQueue.Count > _maxQueueSize)
                {
                    LogManager.GetCurrentClassLogger().Warn("Exceeded maximum queue depth");
                }
            }
        }

        // 
        // Pull off messages from the Queue, batch them up and send them all across
        // 
        private void StatsDSender()
        {
            DateTime lastFlushTime = DateTime.MinValue;

            using (var syncHandle = new ManualResetEventSlim())
            {
                // Execute the query
                while (!Stop)
                {
                    if (!CancelToken.IsCancellationRequested)
                    {
                        try
                        {
                            int messageCount = 0;
                            List<JObject> messages = new List<JObject>();

                            // Lets get whats in the queue
                            lock (_locker)
                            {
                                messageCount = _jsonQueue.Count;

                                // Time to flush?                             
                                if (messageCount >= _flushSize || (DateTime.UtcNow - lastFlushTime).Seconds >= _idleFlushTimeSeconds)
                                {
                                    messages = _jsonQueue.Take(messageCount).ToList();
                                    _jsonQueue.RemoveRange(0, messageCount);
                                    if (messages.Count > 0)
                                        _manager.IncrementMessageCount(messages.Count);
                                }
                            }

                            TransmitStats(messages);

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
                            Interlocked.Increment(ref _errorCount);
                            LogManager.GetCurrentClassLogger().Error(ex);
                        }
                    }
                }
            }
        }

        protected string ExpandField(string fieldName, JObject json)
        {
            foreach (var token in json.Children())
            {
                string replaceString = "%{" + token.Path + "}";
                fieldName = fieldName.Replace(replaceString, json[token.Path].ToString());
            }
            return fieldName;
        }

        private string BuildMetricPath(string metric, JObject json)
        {
            return string.Format("{0}.{1}.{2}", ExpandField(_params.Namespace, json), ExpandField(_params.Sender, json), ExpandField(metric, json));
        }
       
        private void TransmitStats(List<JObject> messages)
        {
            // We've got some to send.
            if (messages.Count > 0)
            {
                do
                {
                    try
                    {
                        int numMessages = messages.Count;
                        foreach (var m in messages)
                        {
                            SendMetrics(m);
                        }
                        messages.RemoveRange(0, numMessages);
                        Interlocked.Add(ref _sentMessages, numMessages);  
                    }
                    catch (Exception ex)
                    {
                        LogManager.GetCurrentClassLogger().Error(ex);
                       
                        interlockedInsert(messages);  // Put the messages back into the queue
                        break;
                    }
                } while (messages.Count > 0);
            }
        }

        // Process all the metrics for this json
        private void SendMetrics(JObject m)
        {
            if (_params.Gauges != null && _params.Gauges.Length > 0)
                DoGauges(m);
            if (_params.Counts != null && _params.Counts.Length > 0)
                DoCounts(m);
            if (_params.Timings != null && _params.Timings.Length > 0)
                DoTimings(m);
            if (_params.Increments != null && _params.Increments.Length > 0)
                DoIncrements(m);
            if (_params.Decrements != null && _params.Decrements.Length > 0)
                DoDecrements(m);
        }

        // Process the Gauges
        private void DoGauges(JObject json)
        {
            for (int i=0; i<_params.Gauges.Length; i += 2)
            {
                string metricPath = BuildMetricPath(_params.Gauges[i], json);
                string gaugeName = ExpandField(_params.Gauges[i + 1], json);
                int value;
                if (int.TryParse(gaugeName, out value))
                {
                    NStatsD.Client.Current.Gauge(metricPath, value, _params.SampleRate);
                }
            }
        }

        // Process the Gauges
        private void DoTimings(JObject json)
        {
            for (int i = 0; i < _params.Timings.Length; i += 2)
            {
                string metricPath = BuildMetricPath(_params.Timings[i], json);
                string timingName = ExpandField(_params.Timings[i + 1], json);
                long value;
                if (long.TryParse(timingName, out value))
                {
                    NStatsD.Client.Current.Timing(metricPath, value, _params.SampleRate);
                }
            }
        }

        // Process the Counts
        private void DoCounts(JObject json)
        {
            for (int i = 0; i < _params.Counts.Length; i += 2)
            {
                string metricPath = BuildMetricPath(_params.Counts[i], json);
                string countName = ExpandField(_params.Counts[i + 1], json);
                int value;
                if (int.TryParse(countName, out value))
                {
                    NStatsD.Client.Current.UpdateStats(metricPath, value, _params.SampleRate);
                }
            }
        }
        // Process the Increments
        private void DoIncrements(JObject json)
        {
            foreach (var metric in _params.Increments)
            {
                string metricPath = BuildMetricPath(metric, json);
                NStatsD.Client.Current.Increment(metricPath, _params.SampleRate);
            }
        }

        // Process the Increments
        private void DoDecrements(JObject json)
        {
            foreach (var metric in _params.Increments)
            {
                string metricPath = BuildMetricPath(metric, json);
                NStatsD.Client.Current.Decrement(metricPath, _params.SampleRate);
            }
        }
    }
}

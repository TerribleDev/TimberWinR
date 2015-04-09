using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Elasticsearch.Net;
using Elasticsearch.Net.ConnectionPool;
using Nest;
using Newtonsoft.Json.Linq;
using NLog;
using RapidRegex.Core;
using RestSharp;
using System.Text.RegularExpressions;
using Elasticsearch.Net.Serialization;
using Newtonsoft.Json;

namespace TimberWinR.Outputs
{
    public class Person
    {
        public string Firstname { get; set; }
        public string Lastname { get; set; }
    }

    public partial class ElasticsearchOutput : OutputSender
    {
        private TimberWinR.Manager _manager;
        private readonly int _port;
        private readonly int _interval;
        private readonly int _flushSize;
        private readonly int _idleFlushTimeSeconds;
        private readonly string[] _hosts;
        private readonly string _protocol;      
        private readonly int _timeout;
        private readonly object _locker = new object();
        private readonly List<JObject> _jsonQueue;
        private readonly int _numThreads;
        private long _sentMessages;
        private long _errorCount;
        private readonly int _maxQueueSize;
        private readonly bool _queueOverflowDiscardOldest;    
        private Parser.ElasticsearchOutputParameters _parameters;
        public bool Stop { get; set; }
      
        /// <summary>
        /// Get the bulk connection pool of hosts
        /// </summary>
        /// <returns></returns>
        private ElasticClient getClient()
        {
            var nodes = new List<Uri>();
            foreach (var host in _hosts)
            {
                var url = string.Format("http://{0}:{1}", host, _port);
                nodes.Add(new Uri(url));
            }
            var pool = new StaticConnectionPool(nodes.ToArray());
            var settings = new ConnectionSettings(pool)
                .ExposeRawResponse();

            var client = new ElasticClient(settings);
            return client;
        }

        public ElasticsearchOutput(TimberWinR.Manager manager, Parser.ElasticsearchOutputParameters parameters, CancellationToken cancelToken)
            : base(cancelToken, "Elasticsearch")
        {
            _sentMessages = 0;
            _errorCount = 0;

            _parameters = parameters;
            _flushSize = parameters.FlushSize;
            _idleFlushTimeSeconds = parameters.IdleFlushTimeInSeconds;
            _protocol = parameters.Protocol;
            _timeout = parameters.Timeout;
            _manager = manager;
            _port = parameters.Port;
            _interval = parameters.Interval;
            _hosts = parameters.Host;          
            _jsonQueue = new List<JObject>();
            _numThreads = parameters.NumThreads;
            _maxQueueSize = parameters.MaxQueueSize;
            _queueOverflowDiscardOldest = parameters.QueueOverflowDiscardOldest;
      

            for (int i = 0; i < parameters.NumThreads; i++)
            {
                Task.Factory.StartNew(ElasticsearchSender, cancelToken, TaskCreationOptions.LongRunning, TaskScheduler.Current);
            }
        }

        public override JObject ToJson()
        {
            JObject json = new JObject(
                new JProperty("elasticsearch",
                    new JObject(
                        new JProperty("host", string.Join(",", _hosts)),
                        new JProperty("errors", _errorCount),
                        new JProperty("sentMmessageCount", _sentMessages),
                        new JProperty("queuedMessageCount", _jsonQueue.Count),
                        new JProperty("port", _port),
                        new JProperty("flushSize", _flushSize),                                       
                        new JProperty("idleFlushTime", _idleFlushTimeSeconds),     
                        new JProperty("interval", _interval),
                        new JProperty("threads", _numThreads),
                        new JProperty("maxQueueSize", _maxQueueSize),
                        new JProperty("overflowDiscardOldest", _queueOverflowDiscardOldest),               
                        new JProperty("hosts",
                            new JArray(
                                from h in _hosts
                                select new JObject(
                                    new JProperty("host", h)))))));
            return json;
        }
        // 
        // Pull off messages from the Queue, batch them up and send them all across
        // 
        private void ElasticsearchSender()
        {
            // Force an inital flush
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

                            // We have some messages to work with
                            if (messages.Count > 0)
                            {
                                var client = getClient();

                                LogManager.GetCurrentClassLogger()
                                    .Debug("Sending {0} Messages to {1}", messages.Count, string.Join(",", _hosts));
                                // This loop will process all messages we've taken from the queue
                                // that have the same index and type (an elasticsearch requirement)
                                do
                                {
                                    try
                                    {
                                        // Grab all messages with same index and type (this is the whole point, group the same ones) 
                                        var bulkTypeName = this._parameters.GetTypeName(messages[0]);
                                        var bulkIndexName = this._parameters.GetIndexName(messages[0]);

                                        IEnumerable<JObject> bulkItems =
                                            messages.TakeWhile(
                                                message =>
                                                    String.Compare(bulkTypeName, _parameters.GetTypeName(message), false) == 0 &&
                                                    String.Compare(bulkIndexName, _parameters.GetIndexName(message), false) == 0);

                                        // Send the message(s), if the are successfully sent, they
                                        // are removed from the queue
                                        lastFlushTime = transmitBulkData(bulkItems, bulkIndexName, bulkTypeName, client, lastFlushTime, messages);
                                        
                                        GC.Collect();
                                    }
                                    catch (Exception ex)
                                    {
                                        LogManager.GetCurrentClassLogger().Error(ex);
                                        break;
                                    }
                                } while (messages.Count > 0);
                            }
                            GC.Collect();
                            if (!Stop)
                            {
                                syncHandle.Wait(TimeSpan.FromMilliseconds(_interval), CancelToken);
                            }
                        }
                        catch (OperationCanceledException)
                        {
                            break;
                        }
                        catch (Exception ex)
                        {
                            LogManager.GetCurrentClassLogger().Error(ex);
                        }
                    }
                }
            }
        }

        //
        // Send the messages to Elasticsearch (bulk)
        //
        private DateTime transmitBulkData(IEnumerable<JObject> bulkItems, string bulkIndexName, string bulkTypeName,
            ElasticClient client, DateTime lastFlushTime, List<JObject> messages)
        {
            var bulkRequest = new BulkRequest() {Refresh = true};
            bulkRequest.Operations = new List<IBulkOperation>();
            foreach (var json in bulkItems)
            {
                // ES requires a timestamp, add one if not present
                var ts = json["@timestamp"];
                if (ts == null)
                    json["@timestamp"] = DateTime.UtcNow;
                var bi = new BulkIndexOperation<JObject>(json);
                bi.Index = bulkIndexName;
                bi.Type = bulkTypeName;
                bulkRequest.Operations.Add(bi);
            }

            // The total messages processed for this operation.
            int numMessages = bulkItems.Count();
     
            var response = client.Bulk(bulkRequest);
            if (!response.IsValid)
            {
                LogManager.GetCurrentClassLogger().Error("Failed to send: {0}", response);
                Interlocked.Increment(ref _errorCount);
                interlockedInsert(messages);  // Put the messages back into the queue
            }
            else // Success!
            {
                lastFlushTime = DateTime.UtcNow;
                LogManager.GetCurrentClassLogger()
                    .Info("Successfully sent {0} messages in a single bulk request", numMessages);
                Interlocked.Add(ref _sentMessages, numMessages);                              
            }

            // Remove them from the working list
            messages.RemoveRange(0, numMessages);
            return lastFlushTime;
        }

        // Places messages back into the queue (for a future attempt)
        private void interlockedInsert(List<JObject> messages)
        {
            lock (_locker)
            {
                _jsonQueue.InsertRange(0, messages);
                if (_jsonQueue.Count > _maxQueueSize)
                {
                    LogManager.GetCurrentClassLogger().Warn("Exceeded maximum queue depth");
                }
            }
        }


        protected override void MessageReceivedHandler(Newtonsoft.Json.Linq.JObject jsonMessage)
        {
            if (_manager.Config.Filters != null)
                ApplyFilters(jsonMessage);

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

        private void ApplyFilters(JObject json)
        {
            foreach (var filter in _manager.Config.Filters)
            {
                filter.Apply(json);
            }
        }

    }
}

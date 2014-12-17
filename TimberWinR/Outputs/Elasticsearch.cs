using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using NLog;
using RapidRegex.Core;
using RestSharp;

namespace TimberWinR.Outputs
{
    using System.Text.RegularExpressions;

    public partial class ElasticsearchOutput : OutputSender
    {
        private TimberWinR.Manager _manager;
        private readonly int _port;
        private readonly int _interval;
        private readonly string[] _host;
        private readonly string _protocol;
        private int _hostIndex;
        private readonly int _timeout;
        private readonly object _locker = new object();
        private readonly List<JObject> _jsonQueue;
        private readonly int _numThreads;
        private long _sentMessages;
        private long _errorCount;
        private Parser.ElasticsearchOutput eo;

        public ElasticsearchOutput(TimberWinR.Manager manager, Parser.ElasticsearchOutput eo, CancellationToken cancelToken)
            : base(cancelToken, "Elasticsearch")
        {
            _sentMessages = 0;
            _errorCount = 0;

            this.eo = eo;
            _protocol = eo.Protocol;
            _timeout = eo.Timeout;
            _manager = manager;
            _port = eo.Port;
            _interval = eo.Interval;
            _host = eo.Host;
            _hostIndex = 0;
            _jsonQueue = new List<JObject>();
            _numThreads = eo.NumThreads;

    for (int i = 0; i < eo.NumThreads; i++)
    {
        Task.Factory.StartNew(ElasticsearchSender, cancelToken, TaskCreationOptions.LongRunning, TaskScheduler.Current);
    }
        }

        public override JObject ToJson()
        {
            JObject json = new JObject(
                new JProperty("elasticsearch",
                    new JObject(
                        new JProperty("host", string.Join(",", _host)),
                        new JProperty("errors", _errorCount),                       
                        new JProperty("sent_messages", _sentMessages),
                        new JProperty("queued_messages", _jsonQueue.Count),
                        new JProperty("port", _port),
                        new JProperty("interval", _interval),
                        new JProperty("threads", _numThreads),                                             
                        new JProperty("hosts",
                            new JArray(
                                from h in _host
                                select new JObject(
                                    new JProperty("host", h)))))));
            return json;
        }
        // 
        // Pull off messages from the Queue, batch them up and send them all across
        // 
        private void ElasticsearchSender()
        {
            while (!CancelToken.IsCancellationRequested)
            {
                JObject[] messages;
                lock (_locker)
                {
                    var count = _jsonQueue.Count;
                    messages = _jsonQueue.Take(count).ToArray();
                    _jsonQueue.RemoveRange(0, count);
                    if (messages.Length > 0)
                        _manager.IncrementMessageCount(messages.Length);
                }

                if (messages.Length > 0)
                {
                    int numHosts = _host.Length;
                    while (numHosts-- > 0)
                    {
                        try
                        {
                            // Get the next client
                            RestClient client = getClient();                           
                            if (client != null)
                            {                              
                                LogManager.GetCurrentClassLogger()
                                    .Debug("Sending {0} Messages to {1}", messages.Length, client.BaseUrl);

                                foreach (JObject json in messages)
                                {
                                    var typeName = this.eo.GetTypeName(json);
                                    var indexName = this.eo.GetIndexName(json);
                                    var req = new RestRequest(string.Format("/{0}/{1}/", indexName, typeName), Method.POST);                                                                                                 

                                    req.AddParameter("text/json", json.ToString(), ParameterType.RequestBody);

                                    req.RequestFormat = DataFormat.Json;

                                    try
                                    {
                                        client.ExecuteAsync(req, response =>
                                        {
                                            if (response.StatusCode != HttpStatusCode.Created)
                                            {
                                                LogManager.GetCurrentClassLogger()
                                                    .Error("Failed to send: {0}", response.ErrorMessage);
                                                Interlocked.Increment(ref _errorCount);
                                            }
                                            else
                                            {
                                                _sentMessages++;
                                                GC.Collect();
                                            }
                                        });
                                    }
                                    catch (Exception error)
                                    {
                                        LogManager.GetCurrentClassLogger().Error(error);
                                        Interlocked.Increment(ref _errorCount);
                                    }                                   
                                }
                                GC.Collect();
                            }
                            else
                            {
                                LogManager.GetCurrentClassLogger()
                                    .Fatal("Unable to connect with any Elasticsearch hosts, {0}",
                                        String.Join(",", _host));
                                Interlocked.Increment(ref _errorCount);
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

        private RestClient getClient()
         {
             if (_hostIndex >= _host.Length)
                _hostIndex = 0;

            int numTries = 0;
            while (numTries < _host.Length)
            {
                try
                {
                    string url = string.Format("{0}://{1}:{2}", _protocol.Replace(":",""), _host[_hostIndex], _port);
                    var client = new RestClient(url);
                    client.Timeout = _timeout;                  

                    _hostIndex++;
                    if (_hostIndex >= _host.Length)
                        _hostIndex = 0;

                    return client;
                }
                catch (Exception)
                {
                }
                numTries++;
            }

            return null;
        }


        protected override void MessageReceivedHandler(Newtonsoft.Json.Linq.JObject jsonMessage)
        {
            if (_manager.Config.Filters != null)
                ApplyFilters(jsonMessage);

            var message = jsonMessage.ToString();
            LogManager.GetCurrentClassLogger().Debug(message);

            lock (_locker)
            {
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

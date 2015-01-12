using Newtonsoft.Json.Linq;
using NLog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace TimberWinR.Outputs
{
    public class StdoutOutput : OutputSender
    {
        private TimberWinR.Manager _manager;
        private readonly int _interval;
        private readonly object _locker = new object();
        private readonly List<JObject> _jsonQueue;
        private long _sentMessages;
        public bool Stop { get; set; }

        public StdoutOutput(TimberWinR.Manager manager, Parser.StdoutOutput eo, CancellationToken cancelToken)
            : base(cancelToken, "Stdout")
        {
            _sentMessages = 0;
            _manager = manager;
            _interval = eo.Interval;
            _jsonQueue = new List<JObject>();

            var elsThread = new Task(StdoutSender, cancelToken);
            elsThread.Start();
        }

        public override JObject ToJson()
        {
            JObject json = new JObject(
                new JProperty("stdout",
                    new JObject(
                        new JProperty("sent_messages", _sentMessages))));

            return json;
        }

        // 
        // Pull off messages from the Queue, batch them up and send them all across
        // 
        private void StdoutSender()
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
                            JObject[] messages;
                            lock (_locker)
                            {
                                messages = _jsonQueue.Take(_jsonQueue.Count).ToArray();
                                _jsonQueue.RemoveRange(0, messages.Length);
                            }

                            if (messages.Length > 0)
                            {
                                try
                                {
                                    foreach (JObject obj in messages)
                                    {
                                        Console.WriteLine(obj.ToString());
                                        _sentMessages++;
                                    }
                                }
                                catch (Exception ex)
                                {
                                    LogManager.GetCurrentClassLogger().Error(ex);
                                }
                            }
                            if (!Stop)
                                syncHandle.Wait(TimeSpan.FromMilliseconds(_interval), CancelToken);
                        }
                        catch (OperationCanceledException oce)
                        {
                            break;
                        }
                        catch (Exception)
                        {
                        }
                    }
                }
            }
        }

        protected override void MessageReceivedHandler(Newtonsoft.Json.Linq.JObject jsonMessage)
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
                _jsonQueue.Add(jsonMessage);
            }
        }

        private bool ApplyFilters(JObject json)
        {
            bool drop = false;

            foreach (var filter in _manager.Config.Filters)
            {
                if (!filter.Apply(json))
                    drop = true;
            }

            return drop;
        }

    }
}


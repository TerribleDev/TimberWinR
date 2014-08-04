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

        public StdoutOutput(TimberWinR.Manager manager, Parser.StdoutOutput eo, CancellationToken cancelToken)
            : base(cancelToken)
        {
            _manager = manager;
            _interval = eo.Interval;
            _jsonQueue = new List<JObject>();

            var elsThread = new Task(StdoutSender, cancelToken);
            elsThread.Start();
        }

        // 
        // Pull off messages from the Queue, batch them up and send them all across
        // 
        private void StdoutSender()
        {
            while (!CancelToken.IsCancellationRequested)
            {
                JObject[] messages;
                lock (_locker)
                {
                    messages = _jsonQueue.Take(1).ToArray();
                    _jsonQueue.RemoveRange(0, messages.Length);
                }

                if (messages.Length > 0)
                {
                    try
                    {
                        foreach (JObject obj in messages)
                        {
                            Console.WriteLine(obj.ToString());
                        }
                    }
                    catch (Exception ex)
                    {
                        LogManager.GetCurrentClassLogger().Error(ex);
                    }
                }
                System.Threading.Thread.Sleep(_interval);
            }
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


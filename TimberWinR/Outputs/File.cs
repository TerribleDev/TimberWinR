using System.IO;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NLog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace TimberWinR.Outputs
{
    public class FileOutput : OutputSender
    {
        private TimberWinR.Manager _manager;
        private readonly int _interval;
        private readonly object _locker = new object();
        private readonly List<JObject> _jsonQueue;
        private long _sentMessages;     
        private Parser.FileOutputParameters _arguments;
        public bool Stop { get; set; }

        public FileOutput(TimberWinR.Manager manager, Parser.FileOutputParameters arguments, CancellationToken cancelToken)
            : base(cancelToken, "File")
        {
            _arguments = arguments;
            _sentMessages = 0;
            _manager = manager;
            _interval = arguments.Interval;
            _jsonQueue = new List<JObject>();

            var elsThread = new Task(FileSender, cancelToken);
            elsThread.Start();
        }

        public override JObject ToJson()
        {
            JObject json = new JObject(
                new JProperty("file-output",
                    new JObject(
                        new JProperty("queuedMessageCount", _jsonQueue.Count),
                        new JProperty("sentMessageCount", _sentMessages))));

            return json;
        }

        // 
        // Pull off messages from the Queue, batch them up and send them all across
        // 
        private void FileSender()
        {
           
            using (var syncHandle = new ManualResetEventSlim())
            {
                var fi = new FileInfo(_arguments.FileName);
                if (File.Exists(_arguments.FileName))
                    File.Delete(_arguments.FileName);

                LogManager.GetCurrentClassLogger().Info("File Output Sending To: {0}", fi.FullName);
       
                using (StreamWriter sw = File.AppendText(_arguments.FileName))
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
                                            sw.WriteLine(obj.ToString(_arguments.ToFormat()));                                        
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
                            catch (OperationCanceledException)
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
        }

        protected override void MessageReceivedHandler(Newtonsoft.Json.Linq.JObject jsonMessage)
        {
            if (_manager.Config.Filters != null)
            {
                if (ApplyFilters(jsonMessage))
                    return;
            }

            var message = jsonMessage.ToString();
   
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


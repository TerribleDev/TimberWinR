using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.AccessControl;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.IO;
using Interop.MSUtil;

using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;
using NLog;
using LogQuery = Interop.MSUtil.LogQueryClassClass;
using EventLogInputFormat = Interop.MSUtil.COMEventLogInputContextClassClass;
using LogRecordSet = Interop.MSUtil.ILogRecordset;

namespace TimberWinR.Inputs
{
    /// <summary>
    /// Listen to Windows Event Log
    /// </summary>
    public class WindowsEvtInputListener : InputListener
    {
        private int _pollingIntervalInSeconds = 1;
        private TimberWinR.Parser.WindowsEvent _arguments;
        private long _receivedMessages;
        private List<Thread> _tasks { get; set; }
        public bool Stop { get; set; }

        public WindowsEvtInputListener(TimberWinR.Parser.WindowsEvent arguments, CancellationToken cancelToken)
            : base(cancelToken, "Win32-Eventlog")
        {
            _arguments = arguments;
            _pollingIntervalInSeconds = arguments.Interval;
            _tasks = new List<Thread>();

            foreach (string eventHive in _arguments.Source.Split(','))
            {
                string hive = eventHive.Trim();
                var thread = new Thread(new ParameterizedThreadStart(EventWatcher));
                _tasks.Add(thread);
                thread.Start(eventHive);
            }
        }

        public override void Shutdown()
        {
            Stop = true;
            LogManager.GetCurrentClassLogger().Info("Shutting Down {0}", InputType);           
            base.Shutdown();
        }

        public override JObject ToJson()
        {
            JObject json = new JObject(
                new JProperty("windows_events",
                    new JObject(
                        new JProperty("messages", _receivedMessages),
                        new JProperty("binaryFormat", _arguments.BinaryFormat.ToString()),
                        new JProperty("direction", _arguments.Direction.ToString()),
                        new JProperty("interval", _arguments.Interval),
                        new JProperty("formatMsg", _arguments.FormatMsg),
                        new JProperty("fullEventCode", _arguments.FullEventCode),
                        new JProperty("fullText", _arguments.FullText),
                        new JProperty("msgErrorMode", _arguments.MsgErrorMode.ToString()),
                        new JProperty("stringsSep", _arguments.StringsSep),
                        new JProperty("resolveSIDs", _arguments.ResolveSIDS),
                        new JProperty("iCheckpoint", CheckpointFileName),
                        new JProperty("source", _arguments.Source))));
            return json;
        }

        private void EventWatcher(object ploc)
        {
            string location = ploc.ToString();

            LogQuery oLogQuery = new LogQuery();

            LogManager.GetCurrentClassLogger().Info("WindowsEvent Input Listener Ready");

            // Instantiate the Event Log Input Format object
            var iFmt = new EventLogInputFormat()
            {
                binaryFormat = _arguments.BinaryFormat.ToString(),
                direction = _arguments.Direction.ToString(),
                formatMsg = _arguments.FormatMsg,
                fullEventCode = _arguments.FullEventCode,
                fullText = _arguments.FullText,
                msgErrorMode = _arguments.MsgErrorMode.ToString(),
                stringsSep = _arguments.StringsSep,
                resolveSIDs = _arguments.ResolveSIDS
            };

            oLogQuery = null;

            Dictionary<string, Int64> logFileMaxRecords = new Dictionary<string, Int64>();

            using (var syncHandle = new ManualResetEventSlim())
            {
                // Execute the query
                while (!Stop)
                {
                    // Execute the query
                    if (!CancelToken.IsCancellationRequested)
                    {
                        try
                        {                          
                            oLogQuery = new LogQuery();

                            var qfiles = string.Format("SELECT Distinct [EventLog] FROM {0}", location);
                            var rsfiles = oLogQuery.Execute(qfiles, iFmt);
                            for (; !rsfiles.atEnd(); rsfiles.moveNext())
                            {
                                var record = rsfiles.getRecord();
                                string logName = record.getValue("EventLog") as string;
                                if (!logFileMaxRecords.ContainsKey(logName))
                                {
                                    var qcount = string.Format("SELECT max(RecordNumber) as MaxRecordNumber FROM {0}",
                                        logName);
                                    var rcount = oLogQuery.Execute(qcount, iFmt);
                                    var qr = rcount.getRecord();
                                    var lrn = (Int64)qr.getValueEx("MaxRecordNumber");
                                    logFileMaxRecords[logName] = lrn;
                                }
                            }


                            foreach (string fileName in logFileMaxRecords.Keys.ToList())
                            {
                                var lastRecordNumber = logFileMaxRecords[fileName];
                                var query = string.Format("SELECT * FROM {0} where RecordNumber > {1}", location,
                                    lastRecordNumber);

                                var rs = oLogQuery.Execute(query, iFmt);
                                // Browse the recordset
                                for (; !rs.atEnd(); rs.moveNext())
                                {

                                    var record = rs.getRecord();
                                    var json = new JObject();
                                    foreach (var field in _arguments.Fields)
                                    {
                                        object v = record.getValue(field.Name);
                                        if (field.Name == "Data")
                                            v = ToPrintable(v.ToString());
                                        json.Add(new JProperty(field.Name, v));
                                    }

                                    var lrn = (Int64)record.getValueEx("RecordNumber");
                                    logFileMaxRecords[fileName] = lrn;

                                    record = null;
                                    ProcessJson(json);
                                    _receivedMessages++;
                                    json = null;

                                }
                                // Close the recordset
                                rs.close();
                                rs = null;
                                GC.Collect();
                            }
                            if (!Stop)
                                syncHandle.Wait(TimeSpan.FromSeconds(_pollingIntervalInSeconds), CancelToken);
                        }
                        catch (OperationCanceledException oce)
                        {
                            break;
                        }
                        catch (Exception ex)
                        {
                            LogManager.GetCurrentClassLogger().Error(ex);
                        }
                    }
                }
                Finished();
            }
        }
    }
}

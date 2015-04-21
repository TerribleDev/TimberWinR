using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Newtonsoft.Json.Linq;
using NLog;
using TimberWinR.Parser;
using LogQuery = Interop.MSUtil.LogQueryClassClass;
using EventLogInputFormat = Interop.MSUtil.COMEventLogInputContextClassClass;
using LogRecordSet = Interop.MSUtil.ILogRecordset;
using System.Diagnostics.Eventing.Reader;

namespace TimberWinR.Inputs
{
    /// <summary>
    /// Listen to Windows Event Log
    /// </summary>
    public class WindowsEvtInputListener : InputListener
    {
        private readonly int _pollingIntervalInSeconds = 1;
        private readonly WindowsEvent _arguments;
        private long _receivedMessages;
        private readonly List<Thread> _tasks;
        public bool Stop { get; set; }

        public WindowsEvtInputListener(WindowsEvent arguments, CancellationToken cancelToken)
            : base(cancelToken, "Win32-Eventlog")
        {
            _arguments = arguments;
            _pollingIntervalInSeconds = arguments.Interval;
            _tasks = new List<Thread>();

            foreach (string eventHive in _arguments.Source.Split(','))
            {
                var thread = new Thread(EventWatcher) {Name = "Win32-Eventlog-" + eventHive};
                _tasks.Add(thread);
                thread.Start(eventHive);
            }
        }

        public override void Shutdown()
        {
            Stop = true;
            LogManager.GetCurrentClassLogger().Info("Shutting Down {0}", InputType);
            foreach (var thread in _tasks)
            {
                thread.Join();
            }
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
            EventRecord eventRecord = null;

            LogManager.GetCurrentClassLogger().Info("WindowsEvent Input Listener Ready");

            // Instantiate the Event Log Input Format object
            //var iFmt = new EventLogInputFormat()
            //{
            //    binaryFormat = _arguments.BinaryFormat.ToString(),
            //    direction = _arguments.Direction.ToString(),
            //    formatMsg = _arguments.FormatMsg,
            //    fullEventCode = _arguments.FullEventCode,
            //    fullText = _arguments.FullText,
            //    msgErrorMode = _arguments.MsgErrorMode.ToString(),
            //    stringsSep = _arguments.StringsSep,
            //    resolveSIDs = _arguments.ResolveSIDS
            //};

            var logFileMaxRecords = new Dictionary<string,List<long>>();

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
                            EventLogQuery q = new EventLogQuery(location, PathType.LogName, "*[System]");
                            q.ReverseDirection = true;
                            q.TolerateQueryErrors = true;
                            EventLogReader reader = new EventLogReader(q);
                            
                            for (eventRecord = reader.ReadEvent(); null != eventRecord; eventRecord = reader.ReadEvent())
                            {
                                if(eventRecord.RecordId != null)
                                {
                                    long latestRecord = eventRecord.RecordId.Value;

                                    if (!logFileMaxRecords.ContainsKey(eventRecord.MachineName) & eventRecord.MachineName != null)
                                    {
                                        logFileMaxRecords.Add(eventRecord.MachineName, new List<long>());
                                    }

                                    if (logFileMaxRecords.ContainsKey(eventRecord.MachineName))
                                    {
                                        if (!logFileMaxRecords[eventRecord.MachineName].Contains(latestRecord))
                                        {
                                            logFileMaxRecords[eventRecord.MachineName].Add(latestRecord);
                                            var json = new JObject();
                                            try
                                            {
                                                json.Add(new JProperty("EventLog", eventRecord.LogName));
                                            }
                                            catch (Exception ex)
                                            {

                                            }

                                            try
                                            {
                                                json.Add(new JProperty("RecordNumber", eventRecord.RecordId));
                                            }
                                            catch (Exception ex)
                                            {

                                            }

                                            try
                                            {
                                                json.Add(new JProperty("TimeGenerated", ((DateTime)eventRecord.TimeCreated).ToUniversalTime()));
                                            }
                                            catch (Exception ex)
                                            {

                                            }


                                            try
                                            {
                                                json.Add(new JProperty("EventID", eventRecord.Id));
                                            }
                                            catch (Exception ex)
                                            {

                                            }

                                            try
                                            {
                                                json.Add(new JProperty("EventTypeName", System.Enum.GetName(typeof(StandardEventLevel), eventRecord.Level)));
                                            }
                                            catch (Exception ex)
                                            {

                                            }

                                            try
                                            {
                                                json.Add(new JProperty("SourceName", eventRecord.ProviderName));
                                            }
                                            catch (Exception ex)
                                            {
                                            }

                                            try
                                            {
                                                json.Add(new JProperty("ComputerName", eventRecord.MachineName));
                                            }
                                            catch (Exception ex)
                                            {

                                            }

                                            try
                                            {
                                                json.Add(new JProperty("Message", eventRecord.FormatDescription()));
                                            }
                                            catch (Exception ex)
                                            {

                                            }

                                            ProcessJson(json);
                                            _receivedMessages++;
                                        }
                                        else
                                        {
                                            break;
                                        }
                                    }
                                }                                
                                GC.Collect();
                            //var oLogQuery = new LogQuery();                            
                            //var qfiles = string.Format("SELECT Distinct [EventLog] FROM {0}", location);
                            //var rsfiles = oLogQuery.Execute(qfiles, iFmt);
                            //for (; !rsfiles.atEnd(); rsfiles.moveNext())
                            //{                                
                            //    var record = rsfiles.getRecord();
                            //    string logName = record.getValue("EventLog") as string;
                            //    if (!logFileMaxRecords.ContainsKey(logName))
                            //    {
                            //        var qcount = string.Format("SELECT max(RecordNumber) as MaxRecordNumber FROM {0}",
                            //            logName);
                            //        var rcount = oLogQuery.Execute(qcount, iFmt);
                            //        var qr = rcount.getRecord();
                            //        var lrn = (Int64)qr.getValueEx("MaxRecordNumber");
                            //        logFileMaxRecords[logName] = lrn;
                            //    }
                            //}


                            //foreach (string fileName in logFileMaxRecords.Keys.ToList())
                            //{
                            //    var lastRecordNumber = logFileMaxRecords[fileName];
                            //    var query = string.Format("SELECT * FROM {0} where RecordNumber > {1}", location,
                            //        lastRecordNumber);

                            //    var rs = oLogQuery.Execute(query, iFmt);
                            //    // Browse the recordset
                            //    for (; !rs.atEnd(); rs.moveNext())
                            //    {
                            //        var record = rs.getRecord();
                            //        var json = new JObject();
                            //        foreach (var field in _arguments.Fields)
                            //        {
                            //            object v = record.getValue(field.Name);
                            //            if (field.Name == "Data")
                            //                v = ToPrintable(v.ToString());
                            //            if ((field.Name == "TimeGenerated" || field.Name == "TimeWritten") && field.DataType == typeof (DateTime))
                            //                v = ((DateTime) v).ToUniversalTime();
                            //            json.Add(new JProperty(field.Name, v));
                            //        }

                            //        var lrn = (Int64)record.getValueEx("RecordNumber");
                            //        logFileMaxRecords[fileName] = lrn;

                            //        ProcessJson(json);
                            //        _receivedMessages++;
                            //    }
                                // Close the recordset
                                //rs.close();                           
                                
                            }
                            if (!Stop)
                                syncHandle.Wait(TimeSpan.FromSeconds(_pollingIntervalInSeconds), CancelToken);
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
                Finished();
            }
        }
    }
}

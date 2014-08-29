using System;
using System.Collections.Generic;
using System.Linq;
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

        public WindowsEvtInputListener(TimberWinR.Parser.WindowsEvent arguments, CancellationToken cancelToken, int pollingIntervalInSeconds = 5)
            : base(cancelToken, "Win32-Eventlog")
        {
            _arguments = arguments;
            _pollingIntervalInSeconds = pollingIntervalInSeconds;

            foreach (string eventHive in _arguments.Source.Split(','))
            {
                string hive = eventHive.Trim();
                Task.Factory.StartNew(() => EventWatcher(eventHive));
            }
        }

        public override void Shutdown()
        {
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

        private void EventWatcher(string location)
        {
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

            var qcount  = string.Format("SELECT max(RecordNumber) as MaxRecordNumber FROM {0}", location);
            var rcount = oLogQuery.Execute(qcount, iFmt);
            var qr = rcount.getRecord();
            var lastRecordNumber = qr.getValueEx("MaxRecordNumber");

            oLogQuery = null;
                    
            // Execute the query
            while (!CancelToken.IsCancellationRequested)
            {
                try
                {
                    oLogQuery = new LogQuery();
                    var query = string.Format("SELECT * FROM {0} where RecordNumber > {1}", location, lastRecordNumber);
                    
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

                        lastRecordNumber = record.getValue("RecordNumber");

                        record = null;
                        ProcessJson(json);
                        _receivedMessages++;
                        json = null;
                        
                    }
                    // Close the recordset
                    rs.close();
                    rs = null;                                      
                }
                catch (Exception ex)
                {
                    LogManager.GetCurrentClassLogger().Error(ex);                                     
                }
                System.Threading.Thread.Sleep(_pollingIntervalInSeconds * 1000);
            }

            Finished();
        }
    }
}

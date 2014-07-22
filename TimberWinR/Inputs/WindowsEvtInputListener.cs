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
        private TimberWinR.Configuration.WindowsEvent _arguments;

        public WindowsEvtInputListener(TimberWinR.Configuration.WindowsEvent arguments, CancellationToken cancelToken, int pollingIntervalInSeconds = 1)
            : base(cancelToken)
        {
            _arguments = arguments;
            _pollingIntervalInSeconds = pollingIntervalInSeconds;
            var task = new Task(EventWatcher, cancelToken);
            task.Start();
        }

        private void EventWatcher()
        {
            var oLogQuery = new LogQuery();

            var checkpointFileName = Path.Combine(System.IO.Path.GetTempPath(),
                string.Format("{0}.lpc", Guid.NewGuid().ToString()));          
          
            // Instantiate the Event Log Input Format object
            var iFmt = new EventLogInputFormat()
            {
                binaryFormat = _arguments.BinaryFormat,
                direction = _arguments.Direction,
                formatMsg = _arguments.FormatMsg,
                fullEventCode = _arguments.FullEventCode,
                fullText = _arguments.FullText,
                msgErrorMode =  _arguments.MsgErrorMode,
                stringsSep = _arguments.StringsSep,
                resolveSIDs = _arguments.ResolveSIDS,
                iCheckpoint = checkpointFileName,               
            };

            // Create the query
            var query = string.Format("SELECT * FROM {0}", _arguments.Source);

            var firstQuery = true;
            // Execute the query
            while (!CancelToken.IsCancellationRequested)
            {
                try
                {
                    var rs = oLogQuery.Execute(query, iFmt);
                    // Browse the recordset
                    for (; !rs.atEnd(); rs.moveNext())
                    {
                        // We want to "tail" the log, so skip the first query results.
                        if (!firstQuery)
                        {
                            var record = rs.getRecord();
                            var json = new JObject();
                            foreach (var field in _arguments.Fields)
                            {
                                object v = record.getValue(field.Name);

                                if (field.FieldType == typeof(DateTime))
                                    v = field.ToDateTime(v).ToUniversalTime();

                                json.Add(new JProperty(field.Name, v));
                            }
                            json.Add(new JProperty("type", "Win32-Eventlog"));
                            ProcessJson(json);
                        }
                    }
                    // Close the recordset
                    rs.close();
                }
                catch (Exception ex)
                {
                    LogManager.GetCurrentClassLogger().Error(ex);
                }
                firstQuery = false;
                System.Threading.Thread.Sleep(_pollingIntervalInSeconds * 1000);
            }
        }       
    }
}

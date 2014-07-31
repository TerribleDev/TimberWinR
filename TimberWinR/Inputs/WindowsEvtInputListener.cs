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
      
        public WindowsEvtInputListener(TimberWinR.Parser.WindowsEvent arguments, CancellationToken cancelToken, int pollingIntervalInSeconds = 1)
            : base(cancelToken, "Win32-Eventlog")
        {          
            _arguments = arguments;
            _pollingIntervalInSeconds = pollingIntervalInSeconds;
            var task = new Task(EventWatcher, cancelToken);
            task.Start();
        }

        public override void Shutdown()
        {
            base.Shutdown();           
        }

        private void EventWatcher()
        {
            var oLogQuery = new LogQuery();

            LogManager.GetCurrentClassLogger().Info("WindowsEvent Input Listener Ready");

            // Instantiate the Event Log Input Format object
            var iFmt = new EventLogInputFormat()
            {
                binaryFormat = _arguments.BinaryFormat.ToString(),
                direction = _arguments.Direction.ToString(),
                formatMsg = _arguments.FormatMsg,
                fullEventCode = _arguments.FullEventCode,
                fullText = _arguments.FullText,
                msgErrorMode =  _arguments.MsgErrorMode.ToString(),
                stringsSep = _arguments.StringsSep,
                resolveSIDs = _arguments.ResolveSIDS,
                iCheckpoint = CheckpointFileName,               
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
                                if (field.Name == "Data")
                                    v = ToPrintable(v.ToString());                               
                                json.Add(new JProperty(field.Name, v));
                            }
                                                      
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

            Finished();
        }       
    }
}

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
        private TimberWinR.Configuration.WindowsEvents _arguments;

        public WindowsEvtInputListener(TimberWinR.Configuration.WindowsEvents arguments, CancellationToken cancelToken, int pollingIntervalInSeconds = 1)
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

            var fileName = Path.Combine(System.IO.Path.GetTempPath(),
                string.Format("{0}.lpc", Guid.NewGuid().ToString()));          
          
            // Instantiate the Event Log Input Format object
            var iFmt = new EventLogInputFormat()
            {
                direction = _arguments.Direction,
                binaryFormat = _arguments.BinaryFormat,
                iCheckpoint = fileName,
                resolveSIDs = _arguments.ResolveSIDS,
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
                            foreach (var fieldName in _arguments.Fields)
                            {
                                object v = record.getValue(fieldName);

                              //  if (field.FieldType == typeof(DateTime))
                              //      v = field.ToDateTime(v).ToUniversalTime();

                                json.Add(new JProperty(fieldName, v));
                            }
                            json.Add(new JProperty("type", "Win32-Eventlog"));
                            ProcessJson(json.ToString());
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

        public static FieldDefinitions FieldDefinitions
        {
            get
            {
                return new FieldDefinitions()
                {               
                    {"EventLog", typeof (string)},
                    {"RecordNumber", typeof (string)},
                    {"TimeGenerated", typeof (DateTime)},
                    {"TimeWritten", typeof (DateTime)},
                    {"EventID", typeof (int)},
                    {"EventType", typeof (int)},
                    {"EventTypeName", typeof (string)},
                    {"EventCategory", typeof (int)},
                    {"EventCategoryName", typeof (string)},
                    {"SourceName", typeof (string)},
                    {"Strings", typeof (string)},
                    {"ComputerName", typeof (string)},
                    {"SID", typeof (string)},
                    {"Message", typeof (string)},
                    {"Data", typeof (string)}
                };
            }
        }
      
    }
}

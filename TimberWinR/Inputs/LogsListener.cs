using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.IO;
using Interop.MSUtil;

using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;
using NLog;

using LogQuery = Interop.MSUtil.LogQueryClassClass;
using TextLineInputFormat = Interop.MSUtil.COMTextLineInputContextClass;
using LogRecordSet = Interop.MSUtil.ILogRecordset;

namespace TimberWinR.Inputs
{
    /// <summary>
    /// Tail a file.
    /// </summary>
    public class LogsListener : InputListener
    {
        private int _pollingIntervalInSeconds;
        private TimberWinR.Parser.Log _arguments;

        public LogsListener(TimberWinR.Parser.Log arguments, CancellationToken cancelToken, int pollingIntervalInSeconds = 3)
            : base(cancelToken, "Win32-FileLog")
        {
            _arguments = arguments;
            _pollingIntervalInSeconds = pollingIntervalInSeconds;
            var task = new Task(FileWatcher, cancelToken);
            task.Start();
        }

        public override void Shutdown()
        {
            base.Shutdown();
        }

        private void FileWatcher()
        {                      
            var iFmt = new TextLineInputFormat()
            {
                iCodepage = _arguments.CodePage,
                splitLongLines = _arguments.SplitLongLines,
                iCheckpoint = CheckpointFileName,
                recurse = _arguments.Recurse
            };

              // Create the query
            var query = string.Format("SELECT * FROM {0}", _arguments.Location);          

            var firstQuery = true;
            // Execute the query
            while (!CancelToken.IsCancellationRequested)
            {
                var oLogQuery = new LogQuery();
                try
                {                   
                    var rs = oLogQuery.Execute(query, iFmt);
                    Dictionary<string, int> colMap = new Dictionary<string, int>();
                    for (int col=0; col<rs.getColumnCount(); col++)
                    {
                        string colName = rs.getColumnName(col);
                        colMap[colName] = col;
                    }

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
                                if (!colMap.ContainsKey(field.Name))
                                    continue;

                                object v = record.getValue(field.Name);
                                if (field.DataType == typeof(DateTime))
                                {
                                    DateTime dt = DateTime.Parse(v.ToString());
                                    json.Add(new JProperty(field.Name, dt));
                                }
                                else
                                    json.Add(new JProperty(field.Name, v));                                
                            }
                            string msg = json["Text"].ToString();
                            if (!string.IsNullOrEmpty(msg))
                                ProcessJson(json);
                        }
                    }
                    // Close the recordset
                    rs.close();
                    rs = null;
                }
                catch (Exception ex)
                {
                    LogManager.GetCurrentClassLogger().Error(ex);
                }
                finally
                {
                    oLogQuery = null;
                }
                firstQuery = false;
                System.Threading.Thread.Sleep(_pollingIntervalInSeconds * 1000);
            }

            Finished();
        }       
    }
}

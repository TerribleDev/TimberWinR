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
    public class TailFileInputListener : InputListener
    {
        private int _pollingIntervalInSeconds = 1;
        private TimberWinR.Parser.Log _arguments;

        public TailFileInputListener(TimberWinR.Parser.Log arguments, CancellationToken cancelToken, int pollingIntervalInSeconds = 1)
            : base(cancelToken, "Win32-FileLog")
        {
            _arguments = arguments;
            _pollingIntervalInSeconds = pollingIntervalInSeconds;
            var task = new Task(FileWatcher, cancelToken);
            task.Start();
        }

        private void FileWatcher()
        {            
            var checkpointFileName = Path.Combine(System.IO.Path.GetTempPath(),
                string.Format("{0}.lpc", Guid.NewGuid().ToString()));

            var iFmt = new TextLineInputFormat()
            {
                iCodepage = _arguments.CodePage,
                splitLongLines = _arguments.SplitLongLines,
                iCheckpoint = checkpointFileName,
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
        }       
    }
}

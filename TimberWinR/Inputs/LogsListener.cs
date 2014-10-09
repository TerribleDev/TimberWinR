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
        private long _receivedMessages;
       
        public LogsListener(TimberWinR.Parser.Log arguments, CancellationToken cancelToken, int pollingIntervalInSeconds = 3)
            : base(cancelToken, "Win32-FileLog")
        {
            _receivedMessages = 0;
            _arguments = arguments;
            _pollingIntervalInSeconds = pollingIntervalInSeconds;

            foreach (string srcFile in _arguments.Location.Split(','))
            {
                string file = srcFile.Trim();
                Task.Factory.StartNew(() => FileWatcher(file));
            }           
        }

        public override void Shutdown()
        {
            base.Shutdown();
        }

        public override JObject ToJson()
        {
            JObject json = new JObject(
                new JProperty("log",
                    new JObject(
                        new JProperty("messages", _receivedMessages),
                        new JProperty("location", _arguments.Location),
                        new JProperty("codepage", _arguments.CodePage),
                        new JProperty("splitLongLines", _arguments.SplitLongLines),                     
                        new JProperty("recurse", _arguments.Recurse)
                        )));
            return json;
        }

        private void FileWatcher(string fileToWatch)
        {                      
            var iFmt = new TextLineInputFormat()
            {
                iCodepage = _arguments.CodePage,
                splitLongLines = _arguments.SplitLongLines,             
                recurse = _arguments.Recurse
            };
        
            Dictionary<string, Int64> logFileMaxRecords = new Dictionary<string, Int64>();
            Dictionary<string, DateTime> logFileCreationTimes = new Dictionary<string, DateTime>();         
        
            // Execute the query
            while (!CancelToken.IsCancellationRequested)
            {
                var oLogQuery = new LogQuery();
                try
                {
                    Thread.CurrentThread.Priority = ThreadPriority.BelowNormal;
                    FileInfo fiw = new FileInfo(fileToWatch);
                    if (!fiw.Exists)
                        continue;

                    var qfiles = string.Format("SELECT Distinct [LogFilename] FROM {0}", fileToWatch);
                    var rsfiles = oLogQuery.Execute(qfiles, iFmt);
                    for (; !rsfiles.atEnd(); rsfiles.moveNext())
                    {
                        var record = rsfiles.getRecord();
                        string logName = record.getValue("LogFilename") as string;
                        FileInfo fi = new FileInfo(logName);
                        DateTime creationTime = fi.CreationTimeUtc;
                        if (!logFileMaxRecords.ContainsKey(logName) || (logFileCreationTimes.ContainsKey(logName) && creationTime > logFileCreationTimes[logName]))
                        {
                            logFileCreationTimes[logName] = creationTime;
                            var qcount = string.Format("SELECT max(Index) as MaxRecordNumber FROM {0}", logName);
                            var rcount = oLogQuery.Execute(qcount, iFmt);
                            var qr = rcount.getRecord();
                            var lrn = (Int64)qr.getValueEx("MaxRecordNumber");
                            logFileMaxRecords[logName] = lrn;
                        }                      
                    }
                    foreach (string fileName in logFileMaxRecords.Keys.ToList())
                    {
                        var lastRecordNumber = logFileMaxRecords[fileName];
                        var query = string.Format("SELECT * FROM {0} where Index > {1}", fileName, lastRecordNumber);
                    
                        var rs = oLogQuery.Execute(query, iFmt);
                        Dictionary<string, int> colMap = new Dictionary<string, int>();
                        for (int col = 0; col < rs.getColumnCount(); col++)
                        {
                            string colName = rs.getColumnName(col);                           
                            colMap[colName] = col;
                        }

                        // Browse the recordset
                        for (; !rs.atEnd(); rs.moveNext())
                        {                           
                            var record = rs.getRecord();
                            var json = new JObject();
                            foreach (var field in _arguments.Fields)
                            {
                                if (!colMap.ContainsKey(field.Name))
                                    continue;

                                object v = record.getValue(field.Name);
                                if (field.DataType == typeof (DateTime))
                                {
                                    DateTime dt = DateTime.Parse(v.ToString());
                                    json.Add(new JProperty(field.Name, dt));
                                }
                                else
                                    json.Add(new JProperty(field.Name, v));
                            }
                            string msg = json["Text"].ToString();
                            if (!string.IsNullOrEmpty(msg))
                            {
                                ProcessJson(json);
                                _receivedMessages++;
                            }

                            var lrn = (Int64)record.getValueEx("Index");
                            logFileMaxRecords[fileName] = lrn;
                        }
                       
                        // Close the recordset
                        rs.close();
                        rs = null;
                    }
                }
                catch (Exception ex)
                {
                    LogManager.GetCurrentClassLogger().Error(ex);
                }
                finally
                {
                    oLogQuery = null;
                }
              
                Thread.CurrentThread.Priority = ThreadPriority.Normal;               
                System.Threading.Thread.Sleep(_pollingIntervalInSeconds * 1000);
            }

            Finished();
        }       
    }
}

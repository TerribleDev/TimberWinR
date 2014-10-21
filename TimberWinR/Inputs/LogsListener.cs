using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.IO;
using Interop.MSUtil;
using Newtonsoft.Json;
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
        private Dictionary<string, Int64> _logFileMaxRecords;
        private Dictionary<string, DateTime> _logFileCreationTimes;
        private Dictionary<string, DateTime> _logFileSampleTimes;
        private Dictionary<string, long> _logFileSizes;


        public LogsListener(TimberWinR.Parser.Log arguments, CancellationToken cancelToken, int pollingIntervalInSeconds = 3)
            : base(cancelToken, "Win32-FileLog")
        {
            _logFileMaxRecords = new Dictionary<string, Int64>();
            _logFileCreationTimes = new Dictionary<string, DateTime>();
            _logFileSampleTimes = new Dictionary<string, DateTime>();
            _logFileSizes = new Dictionary<string, long>();

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
                        new JProperty("type", InputType),
                        new JProperty("location", _arguments.Location),
                        new JProperty("codepage", _arguments.CodePage),
                        new JProperty("splitLongLines", _arguments.SplitLongLines),
                        new JProperty("recurse", _arguments.Recurse),
                        new JProperty("files",
                        new JArray(from f in _logFileMaxRecords.Keys
                                   select new JValue(f))),
                        new JProperty("fileSampleTimes",
                            new JArray(from f in _logFileSampleTimes.Values
                                       select new JValue(f))),                   
                        new JProperty("fileSizes",
                            new JArray(from f in _logFileSizes.Values
                                       select new JValue(f))),
                        new JProperty("fileIndices",
                            new JArray(from f in _logFileMaxRecords.Values
                                       select new JValue(f))),
                        new JProperty("fileCreationDates",
                            new JArray(from f in _logFileCreationTimes.Values
                                       select new JValue(f)))
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


            // Execute the query
            while (!CancelToken.IsCancellationRequested)
            {
                var oLogQuery = new LogQuery();
                try
                {
                    Thread.CurrentThread.Priority = ThreadPriority.BelowNormal;

                    var qfiles = string.Format("SELECT Distinct [LogFilename] FROM {0}", fileToWatch);
                    var rsfiles = oLogQuery.Execute(qfiles, iFmt);
                    for (; !rsfiles.atEnd(); rsfiles.moveNext())
                    {
                        var record = rsfiles.getRecord();
                        string logName = record.getValue("LogFilename") as string;
                        FileInfo fi = new FileInfo(logName);

                        if (!fi.Exists)
                        {
                            _logFileCreationTimes.Remove(logName);
                            _logFileMaxRecords.Remove(logName);
                            _logFileSizes.Remove(logName);
                        }

                        _logFileSampleTimes[logName] = DateTime.UtcNow;

                        DateTime creationTime = fi.CreationTimeUtc;
                        bool logHasRolled = (_logFileCreationTimes.ContainsKey(logName) && creationTime > _logFileCreationTimes[logName]) ||
                                            (_logFileSizes.ContainsKey(logName) && fi.Length < _logFileSizes[logName]);


                        if (!_logFileMaxRecords.ContainsKey(logName) || logHasRolled)
                        {
                            _logFileCreationTimes[logName] = creationTime;
                            _logFileSizes[logName] = fi.Length;
                            var qcount = string.Format("SELECT max(Index) as MaxRecordNumber FROM {0}", logName);
                            var rcount = oLogQuery.Execute(qcount, iFmt);
                            var qr = rcount.getRecord();
                            var lrn = (Int64)qr.getValueEx("MaxRecordNumber");
                            if (logHasRolled)
                            {
                                LogManager.GetCurrentClassLogger().Info("Log {0} has rolled", logName);
                                lrn = 0;
                            }
                            _logFileMaxRecords[logName] = lrn;
                        }

                        _logFileSizes[logName] = fi.Length;
                    }
                    foreach (string fileName in _logFileMaxRecords.Keys.ToList())
                    {
                        var lastRecordNumber = _logFileMaxRecords[fileName];
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
                            {
                                ProcessJson(json);
                                _receivedMessages++;
                            }

                            var lrn = (Int64)record.getValueEx("Index");
                            _logFileMaxRecords[fileName] = lrn;
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

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
using IISW3CLogInputFormat = Interop.MSUtil.COMIISW3CInputContextClassClass;
using LogRecordSet = Interop.MSUtil.ILogRecordset;


namespace TimberWinR.Inputs
{
    public class IISW3CInputListener : InputListener
    {
        private int _pollingIntervalInSeconds = 1;
        private TimberWinR.Parser.IISW3CLog _arguments;
        private long _receivedMessages;

        public IISW3CInputListener(TimberWinR.Parser.IISW3CLog arguments, CancellationToken cancelToken, int pollingIntervalInSeconds = 1)
            : base(cancelToken, "Win32-IISLog")
        {
            _arguments = arguments;
            _receivedMessages = 0;
            _pollingIntervalInSeconds = pollingIntervalInSeconds;
            var task = new Task(IISW3CWatcher, cancelToken);
            task.Start();
        }

        public override void Shutdown()
        {
            base.Shutdown();
        }

        public override JObject ToJson()
        {
            JObject json = new JObject(
                new JProperty("iisw3c",
                    new JObject(
                        new JProperty("messages", _receivedMessages),
                        new JProperty("location", _arguments.Location),
                        new JProperty("codepage", _arguments.CodePage),
                        new JProperty("consolidateLogs", _arguments.ConsolidateLogs),
                        new JProperty("dirTime", _arguments.DirTime),
                        new JProperty("dQuotes", _arguments.DoubleQuotes),
                        new JProperty("recurse", _arguments.Recurse),
                        new JProperty("useDoubleQuotes", _arguments.DoubleQuotes)
                        )));
            return json;
        }


        private void IISW3CWatcher()
        {
            var oLogQuery = new LogQuery();

            var iFmt = new IISW3CLogInputFormat()
            {
                codepage = _arguments.CodePage,
                consolidateLogs = _arguments.ConsolidateLogs,
                dirTime = _arguments.DirTime,
                dQuotes = _arguments.DoubleQuotes,
                iCheckpoint = CheckpointFileName,
                recurse = _arguments.Recurse,
                useDoubleQuotes = _arguments.DoubleQuotes
            };

            if (_arguments.MinDateMod.HasValue)
                iFmt.minDateMod = _arguments.MinDateMod.Value.ToString("yyyy-MM-dd hh:mm:ss");

            // Create the query
            var query = string.Format("SELECT * FROM {0}", _arguments.Location);

            var firstQuery = true;
            // Execute the query
            while (!CancelToken.IsCancellationRequested)
            {
                try
                {
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
                            _receivedMessages++;
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

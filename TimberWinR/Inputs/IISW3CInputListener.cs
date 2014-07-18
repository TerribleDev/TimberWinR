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
        private TimberWinR.Configuration.IISW3CLog _arguments;


        public IISW3CInputListener(TimberWinR.Configuration.IISW3CLog arguments, CancellationToken cancelToken, int pollingIntervalInSeconds = 1)
            : base(cancelToken)
        {
            _arguments = arguments;
            _pollingIntervalInSeconds = pollingIntervalInSeconds;
            var task = new Task(IISW3CWatcher, cancelToken);
            task.Start();
        }

        private void IISW3CWatcher()
        {
            var oLogQuery = new LogQuery();

            var checkpointFileName = Path.Combine(System.IO.Path.GetTempPath(),
                string.Format("{0}.lpc", Guid.NewGuid().ToString()));

            var iFmt = new IISW3CLogInputFormat()
            {
                codepage = _arguments.ICodepage,
                consolidateLogs = _arguments.ConsolidateLogs,
                dirTime = _arguments.DirTime,
                dQuotes = _arguments.DQuotes,
                iCheckpoint = checkpointFileName,               
                recurse = _arguments.Recurse,
                useDoubleQuotes = _arguments.DQuotes
            };

            if (!string.IsNullOrEmpty(_arguments.MinDateMod))
                iFmt.minDateMod = _arguments.MinDateMod;

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

                                if (field.FieldType == typeof(DateTime))
                                    v = field.ToDateTime(v).ToUniversalTime();

                                json.Add(new JProperty(field.Name, v));
                            }
                            json.Add(new JProperty("type", "Win32-IISLog"));
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
    }
}

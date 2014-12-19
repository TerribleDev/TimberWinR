using System;
using System.Collections.Generic;
using System.ComponentModel;
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
using TimberWinR.Parser;
using LogQuery = Interop.MSUtil.LogQueryClassClass;
using W3CLogInputFormat = Interop.MSUtil.COMW3CInputContextClassClass;
using LogRecordSet = Interop.MSUtil.ILogRecordset;


namespace TimberWinR.Inputs
{
    public class W3CInputListener : InputListener
    {
        private readonly int _pollingIntervalInSeconds;
        private readonly TimberWinR.Parser.W3CLog _arguments;
        private long _receivedMessages;
        public bool Stop { get; set; }

        public W3CInputListener(TimberWinR.Parser.W3CLog arguments, CancellationToken cancelToken, int pollingIntervalInSeconds = 5)
            : base(cancelToken, "Win32-W3CLog")
        {
            _arguments = arguments;
            _receivedMessages = 0;
            _pollingIntervalInSeconds = pollingIntervalInSeconds;
            foreach (string loc in _arguments.Location.Split(','))
            {
                string hive = loc.Trim();
                Task.Factory.StartNew(() => IISW3CWatcher(loc));
            }
        }

        public override void Shutdown()
        {
            Stop = true;
            LogManager.GetCurrentClassLogger().Info("Shutting Down {0}", InputType);
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
                        new JProperty("separator", _arguments.Separator),
                        new JProperty("dQuotes", _arguments.DoubleQuotes),
                        new JProperty("dtLines", _arguments.DtLines)
                        )));
            return json;
        }


        private void IISW3CWatcher(string location)
        {
            LogManager.GetCurrentClassLogger().Info("IISW3Listener Ready For {0}", location);

            var oLogQuery = new LogQuery();

            var iFmt = new W3CLogInputFormat()
            {
                codepage = _arguments.CodePage,
                iCodepage = _arguments.CodePage,
                doubleQuotedStrings = _arguments.DoubleQuotes,
                detectTypesLines = _arguments.DtLines,
                dQuotes = _arguments.DoubleQuotes,
                separator = _arguments.Separator
            };

            Dictionary<string, Int64> logFileMaxRecords = new Dictionary<string, Int64>();
            using (var syncHandle = new ManualResetEventSlim())
            {
                // Execute the query
                while (!Stop)
                {
                    // Execute the query
                    if (!CancelToken.IsCancellationRequested)
                    {
                        try
                        {
                            oLogQuery = new LogQuery();

                            var qfiles = string.Format("SELECT Distinct [LogFilename] FROM {0}", location);
                            var rsfiles = oLogQuery.Execute(qfiles, iFmt);
                            for (; !rsfiles.atEnd(); rsfiles.moveNext())
                            {
                                var record = rsfiles.getRecord();
                                string fileName = record.getValue("LogFilename") as string;
                                if (!logFileMaxRecords.ContainsKey(fileName))
                                {
                                    var qcount = string.Format("SELECT max(RowNumber) as MaxRecordNumber FROM {0}",
                                        fileName);
                                    var rcount = oLogQuery.Execute(qcount, iFmt);
                                    var qr = rcount.getRecord();
                                    var lrn = (Int64)qr.getValueEx("MaxRecordNumber");
                                    logFileMaxRecords[fileName] = lrn;
                                }
                            }


                            foreach (string fileName in logFileMaxRecords.Keys.ToList())
                            {
                                var lastRecordNumber = logFileMaxRecords[fileName];
                                var query = string.Format(
                                    "SELECT * FROM '{0}' Where RowNumber > {1} order by RowNumber", fileName,
                                    lastRecordNumber);
                                var rs = oLogQuery.Execute(query, iFmt);
                                var colMap = new Dictionary<string, int>();
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
                                    foreach (var field in colMap.Keys)
                                    {
                                        object v = record.getValue(field);
                                        if (field == "date" || field == "time")
                                        {
                                            DateTime dt = DateTime.Parse(v.ToString());
                                            json.Add(new JProperty(field, dt));
                                        }
                                        else
                                            json.Add(new JProperty(field, v));
                                    }
                                    ProcessJson(json);
                                    _receivedMessages++;
                                    var lrn = (Int64)record.getValueEx("RowNumber");
                                    logFileMaxRecords[fileName] = lrn;
                                    record = null;
                                    json = null;
                                }
                                // Close the recordset
                                rs.close();
                            }
                            if (!Stop)
                                syncHandle.Wait(TimeSpan.FromSeconds(_pollingIntervalInSeconds), CancelToken);
                        }
                        catch (OperationCanceledException oce)
                        {
                            break;
                        }
                        catch (Exception ex)
                        {
                            LogManager.GetCurrentClassLogger().Error(ex);
                        }
                    }
                }
            }

            Finished();
        }
    }
}

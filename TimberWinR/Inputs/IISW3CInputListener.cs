using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Newtonsoft.Json.Linq;
using NLog;
using LogQuery = Interop.MSUtil.LogQueryClassClass;
using IISW3CLogInputFormat = Interop.MSUtil.COMIISW3CInputContextClassClass;
using LogRecordSet = Interop.MSUtil.ILogRecordset;


namespace TimberWinR.Inputs
{
    public class IISW3CInputListener : InputListener
    {
        private readonly int _pollingIntervalInSeconds;
        private readonly Parser.IISW3CLogParameters _arguments;
        private long _receivedMessages;
        public bool Stop { get; set; }
        private IisW3CRowReader rowReader;      

        public IISW3CInputListener(Parser.IISW3CLogParameters arguments, CancellationToken cancelToken, int pollingIntervalInSeconds = 5)
            : base(cancelToken, "Win32-IISLog")
        {          
            _arguments = arguments;
            _receivedMessages = 0;
            _pollingIntervalInSeconds = pollingIntervalInSeconds;
            this.rowReader = new IisW3CRowReader(_arguments.Fields);

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
                        new JProperty("consolidateLogs", _arguments.ConsolidateLogs),
                        new JProperty("dirTime", _arguments.DirTime),
                        new JProperty("dQuotes", _arguments.DoubleQuotes),
                        new JProperty("recurse", _arguments.Recurse),
                        new JProperty("useDoubleQuotes", _arguments.DoubleQuotes)
                        )));
            return json;
        }

        private void IISW3CWatcher(string location)
        {
            LogManager.GetCurrentClassLogger().Info("IISW3Listener Ready For {0}", location);

            var oLogQuery = new LogQuery();

            var iFmt = new IISW3CLogInputFormat()
            {
                codepage = _arguments.CodePage,
                consolidateLogs = true,
                dirTime = _arguments.DirTime,
                dQuotes = _arguments.DoubleQuotes,
                recurse = _arguments.Recurse,
                useDoubleQuotes = _arguments.DoubleQuotes
            };

            if (_arguments.MinDateMod.HasValue)
                iFmt.minDateMod = _arguments.MinDateMod.Value.ToString("yyyy-MM-dd hh:mm:ss");

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
                                    var qcount = string.Format("SELECT max(LogRow) as MaxRecordNumber FROM {0}",
                                        fileName);
                                    var rcount = oLogQuery.Execute(qcount, iFmt);
                                    var qr = rcount.getRecord();
                                    var lrn = (Int64) qr.getValueEx("MaxRecordNumber");
                                    logFileMaxRecords[fileName] = lrn;
                                }
                            }

                            foreach (string fileName in logFileMaxRecords.Keys.ToList())
                            {
                                var lastRecordNumber = logFileMaxRecords[fileName];
                                var query = string.Format("SELECT * FROM '{0}' Where LogRow > {1}", fileName,
                                    lastRecordNumber);

                                var rs = oLogQuery.Execute(query, iFmt);
                                rowReader.ReadColumnMap(rs);

                                // Browse the recordset
                                for (; !rs.atEnd(); rs.moveNext())
                                {
                                    var record = rs.getRecord();
                                    var json = rowReader.ReadToJson(record);
                                    ProcessJson(json);
                                    _receivedMessages++;
                                    var lrn = (Int64) record.getValueEx("LogRow");
                                    logFileMaxRecords[fileName] = lrn;
                                    record = null;
                                    json = null;
                                }
                                // Close the recordset
                                rs.close();
                                GC.Collect();
                            }
                        }
                        catch (OperationCanceledException)
                        {
                            break;
                        }
                        catch (Exception ex)
                        {
                            LogManager.GetCurrentClassLogger().Error(ex);
                        }
                        finally
                        {
                            try
                            {
                                if (!Stop)
                                    syncHandle.Wait(TimeSpan.FromSeconds(_pollingIntervalInSeconds), CancelToken);
                            }
                            catch (Exception)
                            {
                            }
                        }
                    }
                }
            }

            Finished();
        }
    }
}

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Configuration;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.IO;
using Interop.MSUtil;
using Microsoft.Win32;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;

using NLog;
using TimberWinR.Codecs;
using LogQuery = Interop.MSUtil.LogQueryClassClass;
using TextLineInputFormat = Interop.MSUtil.COMTextLineInputContextClass;
using LogRecordSet = Interop.MSUtil.ILogRecordset;
using TimberWinR.Parser;

namespace TimberWinR.Inputs
{
    /// <summary>
    /// Tail a file.
    /// </summary>
    public class LogsListener : InputListener
    {
        private object _locker = new object();
        private int _pollingIntervalInSeconds;
        private TimberWinR.Parser.LogParameters _arguments;
        private long _receivedMessages;
        private CodecArguments _codecArguments;
        private ICodec _codec;
      
        public bool Stop { get; set; }
        public bool IsWildcardFilePattern { get; set; }


        public LogsListener(TimberWinR.Parser.LogParameters arguments, CancellationToken cancelToken)
            : base(cancelToken, "Win32-FileLog")
        {
            Stop = false;
        
            EnsureRollingCaught();

             _codecArguments = arguments.CodecArguments;

            _codecArguments = arguments.CodecArguments;
            if (_codecArguments != null && _codecArguments.Type == CodecArguments.CodecType.multiline)
                _codec = new Multiline(_codecArguments);

            if (!string.IsNullOrEmpty(arguments.Type))
                SetTypeName(arguments.Type);

            _receivedMessages = 0;
            _arguments = arguments;
            _pollingIntervalInSeconds = arguments.Interval;

            IsWildcardFilePattern = arguments.Location.Contains('*');

            foreach (string srcFile in _arguments.Location.Split(','))
            {
                string file = srcFile.Trim();
                string dir = Path.GetDirectoryName(file);
                if (string.IsNullOrEmpty(dir))
                    dir = Environment.CurrentDirectory;
                string fileSpec = Path.Combine(dir, file);

                Task.Factory.StartNew(() => FileWatcher(fileSpec));
            }
        }

        public override void Shutdown()
        {
            LogManager.GetCurrentClassLogger().Info("Shutting Down {0}", InputType);
            Stop = true;
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
                        new JProperty("logSource", _arguments.LogSource),
                        new JProperty("codepage", _arguments.CodePage),
                        new JProperty("splitLongLines", _arguments.SplitLongLines),
                        new JProperty("recurse", _arguments.Recurse),
                        new JProperty("filedb",
                            new JArray(from f in Files.ToList()
                                       select JObject.FromObject(LogsFileDatabase.LookupLogFile(f)))),
                        new JProperty("files",
                            new JArray(from f in Files.ToList()
                                       select new JValue(f)))
                        )));


            if (_codecArguments != null)
            {
                var cp = new JProperty("codec",
                    new JArray(
                        new JObject(
                            new JProperty("type", _codecArguments.Type.ToString()),
                            new JProperty("what", _codecArguments.What.ToString()),
                            new JProperty("negate", _codecArguments.Negate),
                            new JProperty("multilineTag", _codecArguments.MultilineTag),
                            new JProperty("pattern", _codecArguments.Pattern))));
                json.Add(cp);
            }


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

            Dictionary<string, string> _fnfmap = new Dictionary<string, string>();

            using (var syncHandle = new ManualResetEventSlim())
            {
                // Execute the query
                while (!Stop)
                {
                    var oLogQuery = new LogQuery();
                    if (!CancelToken.IsCancellationRequested)
                    {
                        try
                        {
                            var qfiles = string.Format("SELECT Distinct [LogFilename] FROM {0}", fileToWatch);
                            var rsfiles = oLogQuery.Execute(qfiles, iFmt);
                            for (; !rsfiles.atEnd(); rsfiles.moveNext())
                            {
                                var record = rsfiles.getRecord();
                                string logName = record.getValue("LogFilename") as string;
                                FileInfo fi = new FileInfo(logName);                             

                                var dbe = LogsFileDatabase.LookupLogFile(logName);
                    
                                SaveVisitedFileName(dbe.FileName);

                                DateTime creationTime = fi.CreationTimeUtc;
                                bool logHasRolled = dbe.NewFile || (creationTime != dbe.CreationTimeUtc || fi.Length < dbe.LastPosition);

                                if (logHasRolled)
                                {
                                    LogManager.GetCurrentClassLogger().Info("Log {0} has rolled", logName);
                                    LogsFileDatabase.Roll(dbe);
                                }

                                // Log has rolled or this is a new file, or we haven't processed yet.
                                bool processWholeFile = logHasRolled || !dbe.ProcessedFile;

                                if (processWholeFile)
                                    LogsFileDatabase.Update(dbe, true, 0);

                            }
                            rsfiles.close();
                            foreach (string fileName in Files.ToList())
                            {
                                var dbe = LogsFileDatabase.LookupLogFile(fileName);

                                var lastRecordNumber = dbe.LastPosition;
                                var query = string.Format("SELECT * FROM {0} where Index > {1}", fileName,
                                    lastRecordNumber);

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

                                        if (json["logSource"] == null)
                                        {
                                            if (string.IsNullOrEmpty(_arguments.LogSource))
                                                json.Add(new JProperty("logSource", fileName));
                                            else
                                                json.Add(new JProperty("logSource", _arguments.LogSource));
                                        }

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
                                        if (_codecArguments != null && _codecArguments.Type == CodecArguments.CodecType.multiline)
                                        {
                                            _codec.Apply(msg, this);
                                            _receivedMessages++;
                                            dbe.IncrementLineCount();
                                        }
                                        else
                                        {
                                            ProcessJson(json);
                                            dbe.IncrementLineCount();
                                             _receivedMessages++;
                                        }
                                    }

                                    var lrn = (Int64)record.getValueEx("Index");
                                    LogsFileDatabase.Update(dbe, true, lrn);
                                    GC.Collect();
                                }

                                colMap.Clear();
                                // Close the recordset
                                rs.close();
                                rs = null;
                                GC.Collect();

                            }
                        }
                        catch (FileNotFoundException fnfex)
                        {
                            string fn = fnfex.FileName;

                            if (!string.IsNullOrEmpty(fn) && !_fnfmap.ContainsKey(fn))
                            {
                                LogManager.GetCurrentClassLogger().Warn(fnfex.Message);
                                _fnfmap[fn] = fn;
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
                                oLogQuery = null;
                                // Sleep 
                                if (!Stop)
                                    syncHandle.Wait(TimeSpan.FromSeconds(_pollingIntervalInSeconds), CancelToken);
                            }
                            catch (OperationCanceledException)
                            {
                            }
                            catch (Exception ex1)
                            {
                                LogManager.GetCurrentClassLogger().Warn(ex1);
                            }
                        }
                    }
                }
                Finished();
            }
        }
    }
}


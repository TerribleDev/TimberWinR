using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Configuration;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
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
using TimberWinR.Parser;

namespace TimberWinR.Inputs
{
    public class LogsFileDatabase
    {
        private static readonly object _locker = new object();
        private List<LogsFileDatabaseEntry> Entries { get; set; }
        private string DatabaseDirectory { get; set; }
        public string DatabaseFileName
        {
            get { return Path.Combine(DatabaseDirectory, ".timberwinrdb"); }
        }

        public static Manager Manager { get; set; }

        private static LogsFileDatabase instance;

        private bool ExistingFile(string logName)
        {
            lock (_locker)
            {
                return ExistingFileTest(logName);
            }
        }

        private bool ExistingFileTest(string logName)
        {
            var existingEntry = (from e in Entries where e.FileName == logName select e).FirstOrDefault();
            return existingEntry != null;
        }

        private void RemoveFileEntry(string logName)
        {
            lock (_locker)
            {
                var existingEntry = (from e in Entries where e.FileName == logName select e).FirstOrDefault();
                if (existingEntry != null)
                {
                    Entries.Remove(existingEntry);
                    WriteDatabaseFileNoLock();
                }
            }
        }

        private LogsFileDatabaseEntry AddFileEntry(string logName, TextLineInputFormat fmt)
        {
            LogsFileDatabaseEntry de = new LogsFileDatabaseEntry();
            lock (_locker)
            {
                var lq = new LogQuery();
                FileInfo fi = new FileInfo(logName);
                de.FileName = logName;
                de.Size = fi.Length;
                de.SampleTime = DateTime.UtcNow;
                de.CreationTime = fi.CreationTimeUtc;
                if (fi.Exists)
                {
                    var qcount = string.Format("SELECT max(Index) as MaxRecordNumber FROM {0}", logName);
                    var rcount = lq.Execute(qcount, fmt);
                    var qr = rcount.getRecord();
                    var lrn = (Int64)qr.getValueEx("MaxRecordNumber");
                    de.MaxRecords = lrn;
                }
                Entries.Add(de);
                WriteDatabaseFileNoLock();
            }
            return de;
        }

        public static LogsFileDatabaseEntry AddLogFile(string logName, TextLineInputFormat fmt)
        {
            Instance.RemoveFileEntry(logName); // Remove if already exists, otherwise ignores.
            return Instance.AddFileEntry(logName, fmt);
        }

        public static LogsFileDatabase Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = new LogsFileDatabase(Manager.LogfileDir);
                    lock (_locker)
                    {
                        if (!Directory.Exists(instance.DatabaseDirectory))
                        {
                            Directory.CreateDirectory(instance.DatabaseDirectory);
                        }
                        if (File.Exists(instance.DatabaseFileName))
                            instance.ReadDatabaseNoLock();
                        else
                            instance.WriteDatabaseFileNoLock();
                    }
                }
                return instance;
            }
        }

        private void ReadDatabaseNoLock()
        {
            JsonSerializer serializer = new JsonSerializer();
            if (File.Exists(DatabaseFileName))
                Entries = JsonConvert.DeserializeObject<List<LogsFileDatabaseEntry>>(File.ReadAllText(DatabaseFileName));
        }
        private void WriteDatabaseFileNoLock()
        {
            File.WriteAllText(DatabaseFileName, JsonConvert.SerializeObject(instance.Entries), Encoding.UTF8);
        }


        private void ReadDatabaseLock()
        {
            lock (_locker)
            {
                ReadDatabaseNoLock();
            }

        }
        private void WriteDatabaseLock()
        {
            lock (_locker)
            {
                WriteDatabaseFileNoLock();
            }
        }

        private LogsFileDatabase(string databaseDirectory)
        {
            DatabaseDirectory = databaseDirectory;
            Entries = new List<LogsFileDatabaseEntry>();
        }

    }

    public class LogsFileDatabaseEntry
    {
        public string FileName { get; set; }
        public Int64 MaxRecords { get; set; }
        public DateTime CreationTime { get; set; }
        public DateTime SampleTime { get; set; }
        public long Size { get; set; }
    }

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
        private Codec _codec;
        private List<string> _multiline { get; set; }

        public bool Stop { get; set; }

        public LogsListener(TimberWinR.Parser.Log arguments, CancellationToken cancelToken)
            : base(cancelToken, "Win32-FileLog")
        {
            Stop = false;

            _codec = arguments.Codec;
            _logFileMaxRecords = new Dictionary<string, Int64>();
            _logFileCreationTimes = new Dictionary<string, DateTime>();
            _logFileSampleTimes = new Dictionary<string, DateTime>();
            _logFileSizes = new Dictionary<string, long>();

            _receivedMessages = 0;
            _arguments = arguments;
            _pollingIntervalInSeconds = arguments.Interval;

            foreach (string srcFile in _arguments.Location.Split(','))
            {
                string file = srcFile.Trim();
                Task.Factory.StartNew(() => FileWatcher(file));
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


            if (_codec != null)
            {
                var cp = new JProperty("codec",
                    new JArray(
                        new JObject(
                            new JProperty("type", _codec.Type.ToString()),
                            new JProperty("what", _codec.What.ToString()),
                            new JProperty("negate", _codec.Negate),
                            new JProperty("multilineTag", _codec.MultilineTag),
                            new JProperty("pattern", _codec.Pattern))));
                json.Add(cp);
            }


            return json;
        }

        // return true to cancel codec
        private void applyMultilineCodec(string msg)
        {
            if (_codec.Re == null)
                _codec.Re = new Regex(_codec.Pattern);

            Match match = _codec.Re.Match(msg);

            bool isMatch = (match.Success && !_codec.Negate) || (!match.Success && _codec.Negate);

            switch (_codec.What)
            {
                case Codec.WhatType.previous:
                    if (isMatch)
                    {
                        if (_multiline == null)
                            _multiline = new List<string>();

                        _multiline.Add(msg);
                    }
                    else // No Match
                    {
                        if (_multiline != null)
                        {
                            string single = string.Join("\n", _multiline.ToArray());
                            _multiline = null;
                            JObject jo = new JObject();
                            jo["message"] = single;
                            jo.Add("tags", new JArray(_codec.MultilineTag));
                            AddDefaultFields(jo);
                            ProcessJson(jo);
                            _receivedMessages++;
                        }
                        _multiline = new List<string>();
                        _multiline.Add(msg);
                    }
                    break;
                case Codec.WhatType.next:
                    if (isMatch)
                    {
                        if (_multiline == null)
                            _multiline = new List<string>();
                        _multiline.Add(msg);
                    }
                    else // No match
                    {
                        if (_multiline != null)
                        {
                            _multiline.Add(msg);
                            string single = string.Join("\n", _multiline.ToArray());
                            _multiline = null;
                            JObject jo = new JObject();
                            jo["message"] = single;
                            jo.Add("tags", new JArray(_codec.MultilineTag));
                            AddDefaultFields(jo);
                            ProcessJson(jo);
                            _receivedMessages++;
                        }
                        else
                        {
                            JObject jo = new JObject();
                            jo["message"] = msg;
                            AddDefaultFields(jo);
                            ProcessJson(jo);
                            _receivedMessages++;
                        }
                    }
                    break;
            }
        }


        private void FileWatcher(string fileToWatch)
        {
            var iFmt = new TextLineInputFormat()
            {
                iCodepage = _arguments.CodePage,
                splitLongLines = _arguments.SplitLongLines,
                recurse = _arguments.Recurse
            };

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

                                if (!fi.Exists)
                                {
                                    _logFileCreationTimes.Remove(logName);
                                    _logFileMaxRecords.Remove(logName);
                                    _logFileSizes.Remove(logName);
                                }

                                _logFileSampleTimes[logName] = DateTime.UtcNow;

                                DateTime creationTime = fi.CreationTimeUtc;
                                bool logHasRolled = (_logFileCreationTimes.ContainsKey(logName) &&
                                                     creationTime > _logFileCreationTimes[logName]) ||
                                                    (_logFileSizes.ContainsKey(logName) &&
                                                     fi.Length < _logFileSizes[logName]);


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
                            rsfiles.close();
                            foreach (string fileName in _logFileMaxRecords.Keys.ToList())
                            {
                                var lastRecordNumber = _logFileMaxRecords[fileName];
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
                                        if (_codec != null && _codec.Type == Codec.CodecType.multiline)
                                            applyMultilineCodec(msg);
                                        else
                                        {
                                            ProcessJson(json);
                                            _receivedMessages++;
                                        }
                                    }

                                    var lrn = (Int64)record.getValueEx("Index");
                                    _logFileMaxRecords[fileName] = lrn;
                                    GC.Collect();
                                }

                                colMap.Clear();
                                // Close the recordset
                                rs.close();
                                rs = null;
                                GC.Collect();
                            }
                            // Sleep 
                            if (!Stop)
                                syncHandle.Wait(TimeSpan.FromSeconds(_pollingIntervalInSeconds), CancelToken);
                        }
                        catch (FileNotFoundException fnfex)
                        {
                            LogManager.GetCurrentClassLogger().Warn(fnfex.Message);
                        }
                        catch (OperationCanceledException oce)
                        {
                            break;
                        }
                        catch (Exception ex)
                        {
                            LogManager.GetCurrentClassLogger().Error(ex);
                        }
                        finally
                        {
                            oLogQuery = null;
                        }
                    }
                }
                Finished();
            }
        }
    }
}


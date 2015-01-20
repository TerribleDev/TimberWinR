using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Newtonsoft.Json;
using NLog;

using LogQuery = Interop.MSUtil.LogQueryClassClass;
using TextLineInputFormat = Interop.MSUtil.COMTextLineInputContextClass;
using LogRecordSet = Interop.MSUtil.ILogRecordset;

namespace TimberWinR.Inputs
{
    //
    // Maintain persistent state for Log files (to be used across restarts)
    //
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
            var de = new LogsFileDatabaseEntry();
            lock (_locker)
            {
                var lq = new LogQuery();
                var fi = new FileInfo(logName);
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
                            Directory.CreateDirectory(instance.DatabaseDirectory);
                        // If it exists, read the current state, otherwise create an empty database.
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
            try
            {
                var serializer = new JsonSerializer();
                if (File.Exists(DatabaseFileName))
                    Entries =
                        JsonConvert.DeserializeObject<List<LogsFileDatabaseEntry>>(File.ReadAllText(DatabaseFileName));
            }
            catch (Exception ex)
            {
                LogManager.GetCurrentClassLogger()
                    .Error("Error reading database '{0}': {1}", DatabaseFileName, ex.ToString());
            }
        }
        private void WriteDatabaseFileNoLock()
        {
            try
            {
                File.WriteAllText(DatabaseFileName, JsonConvert.SerializeObject(instance.Entries), Encoding.UTF8);
            }
            catch (Exception ex)
            {
                LogManager.GetCurrentClassLogger()
                    .Error("Error saving database '{0}': {1}", DatabaseFileName, ex.ToString());
            }
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

}

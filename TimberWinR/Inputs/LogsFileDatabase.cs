using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NLog;
using TimberWinR.Parser;


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

        //
        // Lookup the database entry for this log file, returns null if there isnt one.
        //
        private LogsFileDatabaseEntry FindFileWithLock(string logName)
        {
            lock (_locker)
            {
                var existingEntry = (from e in Entries where e.FileName == logName select e).FirstOrDefault();
                return existingEntry;
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

        private LogsFileDatabaseEntry AddFileEntryWithLock(string logName)
        {
            var de = new LogsFileDatabaseEntry();
            lock (_locker)
            {
                var fi = new FileInfo(logName);
                de.FileName = logName;
                de.LogFileExists = fi.Exists;
                de.Previous = "";
                de.NewFile = true;
                de.ProcessedFile = false;
                de.LastPosition = fi.Length;
                de.SampleTime = DateTime.UtcNow;
                de.CreationTimeUtc = fi.CreationTimeUtc;

                Entries.Add(de);
                WriteDatabaseFileNoLock();
            }
            return de;
        }

        public static LogsFileDatabaseEntry LookupLogFile(string logName)
        {
            LogsFileDatabaseEntry dbe = Instance.FindFileWithLock(logName);
            if (dbe == null)
                dbe = Instance.AddFileEntryWithLock(logName);

            FileInfo fi = new FileInfo(logName);

            dbe.LogFileExists = fi.Exists;
            var creationTime = fi.CreationTimeUtc;

            if (dbe.LogFileExists && creationTime != dbe.CreationTimeUtc)
            {
                dbe.NewFile = true;
                dbe.Previous = "";
            }
            dbe.CreationTimeUtc = creationTime;

            return dbe;
        }

        // Find all the non-existent entries and remove them.
        private void PruneFilesWithLock()
        {
            lock (_locker)
            {
                foreach (var entry in Entries.ToList())
                {
                    var fi = new FileInfo(entry.FileName);
                    if (!fi.Exists)
                        Entries.Remove(entry);
                }
                WriteDatabaseFileNoLock();
            }
        }

        public static void Update(LogsFileDatabaseEntry dbe, bool processedFile, long lastOffset)
        {
            dbe.ProcessedFile = processedFile;
            dbe.LogFileExists = File.Exists(dbe.FileName);
            Instance.UpdateEntryWithLock(dbe, lastOffset);
        }

        public static void Roll(LogsFileDatabaseEntry dbe)
        {
            dbe.ProcessedFile = false;
            dbe.LastPosition = 0;
            dbe.Previous = "";
            Instance.UpdateEntryWithLock(dbe, 0);
            dbe.NewFile = true;
        }

        private void UpdateEntryWithLock(LogsFileDatabaseEntry dbe, long lastOffset)
        {
            lock (_locker)
            {
                var fi = new FileInfo(dbe.FileName);
                dbe.NewFile = !fi.Exists;
                dbe.CreationTimeUtc = fi.CreationTimeUtc;
                dbe.SampleTime = DateTime.UtcNow;
                dbe.LastPosition = lastOffset;

                WriteDatabaseFileNoLock();
            }
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

                        if (instance.Entries == null)
                            instance.Entries = new List<LogsFileDatabaseEntry>();

                        instance.PruneFilesWithLock();
                    }
                }
                return instance;
            }
        }


        // Serialize in the Database
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
                LogManager.GetCurrentClassLogger().Error("Error reading database '{0}': {1}", DatabaseFileName, ex.ToString());
                try
                {
                    if (File.Exists(DatabaseFileName))
                        File.Delete(DatabaseFileName);
                    LogManager.GetCurrentClassLogger().Error("Creating New Database '{0}'", DatabaseFileName);
                    WriteDatabaseLock();
                }
                catch (Exception ex2)
                {
                    LogManager.GetCurrentClassLogger().Error("Error Creating New Database '{0}': {1}", DatabaseFileName, ex2.ToString());
                }
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


    //
    // Represents a log file to be tailed
    // 
    public class LogsFileDatabaseEntry
    {
        [JsonIgnore]
        public bool NewFile { get; set; }
        public bool ProcessedFile { get; set; }
        public bool LogFileExists { get; set; }
        public string FileName { get; set; }
        public DateTime CreationTimeUtc { get; set; }
        public DateTime SampleTime { get; set; }
        public long LastPosition { get; set; }
        public long LinesProcessed
        {
            get { return _linesProcessed; }
        }

        private int _linesProcessed;
        public void IncrementLineCount()
        {
            Interlocked.Increment(ref _linesProcessed);
        }
        public string Previous { get; set; }
    }

}

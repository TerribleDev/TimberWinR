using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Newtonsoft.Json;
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

        private bool ExistingFile(string logName)
        {
            lock (_locker)
            {
                return ExistingFileTest(logName);
            }
        }

        private LogsFileDatabaseEntry FindFile(string logName)
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

        private LogsFileDatabaseEntry AddFileEntry(string logName)
        {
            var de = new LogsFileDatabaseEntry();
            lock (_locker)
            {
                de.NewFile = true;
                var fi = new FileInfo(logName);
                de.FileName = logName;
                de.Size = fi.Length;
                de.SampleTime = DateTime.UtcNow;
                de.CreationTimeUtc = fi.CreationTimeUtc;
                Entries.Add(de);
                WriteDatabaseFileNoLock();
            }
            return de;
        }

        public static LogsFileDatabaseEntry LookupLogFile(string logName)
        {
            LogsFileDatabaseEntry dbe = Instance.FindFile(logName);
            if (dbe == null)
                dbe = Instance.AddFileEntry(logName);
            else
                dbe.NewFile = false;
    
            return dbe;
        }

        public static void Update(LogsFileDatabaseEntry dbe)
        {
            Instance.UpdateEntry(dbe);
        }

        private void UpdateEntry(LogsFileDatabaseEntry dbe)
        {
            lock(_locker)
            {
                var fi = new FileInfo(dbe.FileName);
                dbe.CreationTimeUtc = fi.CreationTimeUtc;
                dbe.SampleTime = DateTime.UtcNow;
                dbe.Size = fi.Length;
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
                try
                {
                    if (File.Exists(DatabaseFileName))
                        File.Delete(DatabaseFileName);
                    LogManager.GetCurrentClassLogger().Info("Creating New Database '{0}'", DatabaseFileName);
                    WriteDatabaseLock();
                }
                catch (Exception ex2)
                {
                    LogManager.GetCurrentClassLogger().Info("Error Creating New Database '{0}': {1}", DatabaseFileName, ex2.ToString());
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

    public class LogsFileDatabaseEntry
    {
        [JsonIgnore]
        public bool NewFile { get; set; }
        public string FileName { get; set; }
        public Int64 MaxRecords { get; set; }
        public DateTime CreationTimeUtc { get; set; }
        public DateTime SampleTime { get; set; }
        public long Size { get; set; }       
    }

}

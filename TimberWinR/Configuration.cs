using System;
using System.Collections.Generic;
using System.Data.Odbc;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Text;
using System.Xml;
using System.Xml.Linq;
using System.IO;
using System.Globalization;
using System.Xml.Schema;
using Newtonsoft.Json;
using Newtonsoft.Json.Bson;
using Newtonsoft.Json.Linq;
using TimberWinR.Inputs;
using TimberWinR.Filters;


using NLog;
using TimberWinR.Parser;
using Topshelf.Configurators;
using WindowsEvent = TimberWinR.Parser.WindowsEvent;

namespace TimberWinR
{
    public class Configuration
    {       
        private CancellationToken _cancelToken;
        
        private FileSystemWatcher _dirWatcher;
        private Manager _manager;
 
        private List<WindowsEvent> _events = new List<WindowsEvent>();
        public IEnumerable<WindowsEvent> Events
        {
            get { return _events; }
        }

        private List<StatsDOutputParameters> _statsdOutputs = new List<StatsDOutputParameters>();
        public IEnumerable<StatsDOutputParameters> StatsDOutputs
        {
            get { return _statsdOutputs; }
        }

        private List<RedisOutputParameters> _redisOutputs = new List<RedisOutputParameters>();
        public IEnumerable<RedisOutputParameters> RedisOutputs
        {
            get { return _redisOutputs; }
        }
       

        private List<ElasticsearchOutputParameters> _elasticsearchOutputs = new List<ElasticsearchOutputParameters>();
        public IEnumerable<ElasticsearchOutputParameters> ElasticsearchOutputs
        {
            get { return _elasticsearchOutputs; }
        }

        private List<StdoutOutputParameters> _stdoutOutputs = new List<StdoutOutputParameters>();
        public IEnumerable<StdoutOutputParameters> StdoutOutputs
        {
            get { return _stdoutOutputs; }
        }

        private List<FileOutputParameters> _fileOutputs = new List<FileOutputParameters>();
        public IEnumerable<FileOutputParameters> FileOutputs
        {
            get { return _fileOutputs; }
        }

        private List<TcpParameters> _tcps = new List<TcpParameters>();
        public IEnumerable<TcpParameters> Tcps
        {
            get { return _tcps; }
        }

        private List<UdpParameters> _udps = new List<UdpParameters>();
        public IEnumerable<UdpParameters> Udps
        {
            get { return _udps; }
        }     

        private List<LogParameters> _logs = new List<LogParameters>();
        public IEnumerable<LogParameters> Logs
        {
            get { return _logs; }
        }

        private List<TailFileArguments> _tails = new List<TailFileArguments>();
        public IEnumerable<TailFileArguments> TailFiles
        {
            get { return _tails; }
        }    

        private List<IISW3CLogParameters> _iisw3clogs = new List<IISW3CLogParameters>();

        public IEnumerable<IISW3CLogParameters> IISW3C
        {
            get { return _iisw3clogs; }
        }

        private List<W3CLogParameters> _w3clogs = new List<W3CLogParameters>();

        public IEnumerable<W3CLogParameters> W3C
        {
            get { return _w3clogs; }
        }

        private List<Stdin> _stdins = new List<Stdin>();
        public IEnumerable<Stdin> Stdins
        {
            get { return _stdins; }
        }

        private List<GeneratorParameters> _generators = new List<GeneratorParameters>();
        public IEnumerable<GeneratorParameters> Generators
        {
            get { return _generators; }
        }

        private List<LogstashFilter> _filters = new List<LogstashFilter>();

        public IEnumerable<LogstashFilter> Filters
        {
            get { return _filters; }
        }

        private void MonitorDirectory(string directoryToWatch, CancellationToken cancelToken, Manager manager)
        {
            _manager = manager;
            _cancelToken = cancelToken;
            if (_dirWatcher == null)
            {
                _dirWatcher = new FileSystemWatcher();
                _dirWatcher.Path = directoryToWatch;
                _dirWatcher.NotifyFilter = NotifyFilters.LastAccess | NotifyFilters.LastWrite | NotifyFilters.FileName | NotifyFilters.DirectoryName;
                // Only watch json files.
                _dirWatcher.Filter = "*.json";
                _dirWatcher.Created += DirWatcherOnCreated;
                _dirWatcher.Changed += DirWatcherOnChanged;
                _dirWatcher.Renamed += DirWatcherOnRenamed;
                _dirWatcher.EnableRaisingEvents = true;
            }       
        }

        private void DirWatcherOnRenamed(object sender, RenamedEventArgs e)
        {
            // The Renamed file could be a different name from .json
            FileInfo fi = new FileInfo(e.FullPath);
            if (fi.Extension == ".json")
            {
                LogManager.GetCurrentClassLogger().Info("File: OnRenamed " + e.FullPath + " " + e.ChangeType);
                ProcessNewJson(e.FullPath);
            }
        }

        private void DirWatcherOnCreated(object sender, FileSystemEventArgs e)
        {
            FileInfo fi = new FileInfo(e.FullPath);
            if (fi.Extension == ".json")
            {
                LogManager.GetCurrentClassLogger().Info("File: OnCreated " + e.FullPath + " " + e.ChangeType);
                ProcessNewJson(e.FullPath);
            }
        }

        private void DirWatcherOnChanged(object sender, FileSystemEventArgs e)
        {
            FileInfo fi = new FileInfo(e.FullPath);
            if (fi.Extension == ".json")
            {
                // Specify what is done when a file is changed, created, or deleted.
                LogManager.GetCurrentClassLogger()
                    .Info("File: OnChanged " + e.ChangeType.ToString() + " " + e.FullPath + " " + e.ChangeType);
                ProcessNewJson(e.FullPath);
            }
        }
      
        private void ProcessNewJson(string fileName)
        {
            try
            {
                Configuration c = new Configuration();
                var config = Configuration.FromFile(fileName, c);
                _manager.ProcessConfiguration(_cancelToken, config);

            }
            catch (Exception ex)
            {
                LogManager.GetCurrentClassLogger().Error(ex);                         
            }
        }

        private void ShutdownDirectoryMonitor()
        {           
            _dirWatcher.EnableRaisingEvents = false;
            LogManager.GetCurrentClassLogger().Info("Stopping Directory Monitor");            
        }

        private void DirectoryWatcher(string directoryToWatch)
        {
            LogManager.GetCurrentClassLogger().Info("Starting Directory Monitor {0}", directoryToWatch);                       
        }

        public static Configuration FromDirectory(string jsonDirectory, CancellationToken cancelToken, Manager manager)
        {
            Configuration c = null;
      
            foreach (string jsonConfFile in Directory.GetFiles(jsonDirectory, "*.json"))
            {
                if (!string.IsNullOrEmpty(jsonConfFile))
                {
                    c = FromFile(jsonConfFile, c);
                }
            }

            // Startup Directory Monitor
            if (manager.LiveMonitor)           
                c.MonitorDirectory(jsonDirectory, cancelToken, manager);            

            return c;
        }

        public static Configuration FromFile(string jsonConfFile, Configuration c = null)
        {           
            if (!string.IsNullOrEmpty(jsonConfFile))
            {
                LogManager.GetCurrentClassLogger().Info("Reading Configuration From {0}", jsonConfFile);
                string json = File.ReadAllText(jsonConfFile);

                return FromString(json, c);
            }

            return null;
        }

        public static Configuration FromString(string json, Configuration c = null)
        {
            if (c == null)
                c = new Configuration();
            
            JsonSerializer serializer = new JsonSerializer();
            TextReader re = new StringReader(json);
            JsonTextReader reader = new JsonTextReader(re);

            var x = serializer.Deserialize<TimberWinR.Parser.RootObject>(reader);

            if (x.TimberWinR.Inputs != null)
            {
                if (x.TimberWinR.Inputs.WindowsEvents != null)
                    c._events.AddRange(x.TimberWinR.Inputs.WindowsEvents.ToList());
                if (x.TimberWinR.Inputs.W3CLogs != null)
                    c._w3clogs.AddRange(x.TimberWinR.Inputs.W3CLogs.ToList());
                if (x.TimberWinR.Inputs.IISW3CLogs != null)
                    c._iisw3clogs.AddRange(x.TimberWinR.Inputs.IISW3CLogs.ToList());
                if (x.TimberWinR.Inputs.Stdins != null)
                    c._stdins.AddRange(x.TimberWinR.Inputs.Stdins.ToList());
                if (x.TimberWinR.Inputs.Generators != null)
                    c._generators.AddRange(x.TimberWinR.Inputs.Generators.ToList());
                if (x.TimberWinR.Inputs.Logs != null)
                    c._logs.AddRange(x.TimberWinR.Inputs.Logs.ToList());
                if (x.TimberWinR.Inputs.TailFilesArguments != null)
                    c._tails.AddRange(x.TimberWinR.Inputs.TailFilesArguments.ToList());
                if (x.TimberWinR.Inputs.Tcps != null)
                    c._tcps.AddRange(x.TimberWinR.Inputs.Tcps.ToList());
                if (x.TimberWinR.Inputs.Udps != null)
                    c._udps.AddRange(x.TimberWinR.Inputs.Udps.ToList());
            }

            if (x.TimberWinR.Outputs != null)
            {
                if (x.TimberWinR.Outputs.StatsD != null)
                    c._statsdOutputs.AddRange(x.TimberWinR.Outputs.StatsD.ToList());
                if (x.TimberWinR.Outputs.Redis != null)
                    c._redisOutputs.AddRange(x.TimberWinR.Outputs.Redis.ToList());
                if (x.TimberWinR.Outputs.Elasticsearch != null)
                    c._elasticsearchOutputs.AddRange(x.TimberWinR.Outputs.Elasticsearch.ToList());
                if (x.TimberWinR.Outputs.Stdout != null)
                    c._stdoutOutputs.AddRange(x.TimberWinR.Outputs.Stdout.ToList());
                if (x.TimberWinR.Outputs.File != null)
                    c._fileOutputs.AddRange(x.TimberWinR.Outputs.File.ToList());
            }

            if (x.TimberWinR.Filters != null)
                c._filters.AddRange(x.TimberWinR.AllFilters.ToList());

            c.Validate(c);

            // Validate 
            return c;
        }

        void Validate(Configuration c)
        {
            try
            {
                foreach (var e in c.Events)
                    e.Validate();

                foreach (var f in c.Filters)
                    f.Validate();
            }
            catch (Exception ex)
            {
                LogManager.GetCurrentClassLogger().Error(ex);
                throw ex;
            }          
        }

        public Configuration()
        {
            _filters = new List<LogstashFilter>();
            _events = new List<WindowsEvent>();
            _iisw3clogs = new List<IISW3CLogParameters>();
            _logs = new List<LogParameters>();
            _statsdOutputs = new List<StatsDOutputParameters>();
            _redisOutputs = new List<RedisOutputParameters>();
            _elasticsearchOutputs = new List<ElasticsearchOutputParameters>();
            _stdoutOutputs = new List<StdoutOutputParameters>();
            _fileOutputs = new List<FileOutputParameters>();
            _tcps = new List<TcpParameters>();
            _udps = new List<UdpParameters>();
        }

        public static Object GetPropValue(String name, Object obj)
        {
            foreach (String part in name.Split('.'))
            {
                if (obj == null) { return null; }

                Type type = obj.GetType();
                PropertyInfo info = type.GetProperty(part);
                if (info == null) { return null; }

                obj = info.GetValue(obj, null);
            }
            return obj;
        }           
    }
}
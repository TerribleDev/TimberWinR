using System;
using System.Collections.Generic;
using System.Data.Odbc;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
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
using IISW3CLog = TimberWinR.Parser.IISW3CLog;
using WindowsEvent = TimberWinR.Parser.WindowsEvent;

namespace TimberWinR
{
    public class Configuration
    {
        private List<WindowsEvent> _events = new List<WindowsEvent>();
        public IEnumerable<WindowsEvent> Events
        {
            get { return _events; }
        }

        private List<RedisOutput> _redisOutputs = new List<RedisOutput>();
        public IEnumerable<RedisOutput> RedisOutputs
        {
            get { return _redisOutputs; }
        }


        private List<ElasticsearchOutput> _elasticsearchOutputs = new List<ElasticsearchOutput>();
        public IEnumerable<ElasticsearchOutput> ElasticsearchOutputs
        {
            get { return _elasticsearchOutputs; }
        }

        private List<StdoutOutput> _stdoutOutputs = new List<StdoutOutput>();
        public IEnumerable<StdoutOutput> StdoutOutputs
        {
            get { return _stdoutOutputs; }
        }

        private List<Tcp> _tcps = new List<Tcp>();
        public IEnumerable<Tcp> Tcps
        {
            get { return _tcps; }
        }

        private List<Udp> _udps = new List<Udp>();
        public IEnumerable<Udp> Udps
        {
            get { return _udps; }
        }     

        private List<Log> _logs = new List<Log>();
        public IEnumerable<Log> Logs
        {
            get { return _logs; }
        }

        private List<TailFile> _tails = new List<TailFile>();
        public IEnumerable<TailFile> TailFiles
        {
            get { return _tails; }
        }    

        private List<IISW3CLog> _iisw3clogs = new List<IISW3CLog>();

        public IEnumerable<IISW3CLog> IISW3C
        {
            get { return _iisw3clogs; }
        }

        private List<W3CLog> _w3clogs = new List<W3CLog>();

        public IEnumerable<W3CLog> W3C
        {
            get { return _w3clogs; }
        }

        private List<Stdin> _stdins = new List<Stdin>();
        public IEnumerable<Stdin> Stdins
        {
            get { return _stdins; }
        }

        private List<LogstashFilter> _filters = new List<LogstashFilter>();

        public IEnumerable<LogstashFilter> Filters
        {
            get { return _filters; }
        }


        public static Configuration FromDirectory(string jsonDirectory)
        {
            Configuration c = null;

            foreach (string jsonConfFile in Directory.GetFiles(jsonDirectory, "*.json"))
            {
                if (!string.IsNullOrEmpty(jsonConfFile))
                {
                    c = FromFile(jsonConfFile, c);
                }
            }

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
                if (x.TimberWinR.Inputs.Logs != null)
                    c._logs.AddRange(x.TimberWinR.Inputs.Logs.ToList());
                if (x.TimberWinR.Inputs.TailFiles != null)
                    c._tails.AddRange(x.TimberWinR.Inputs.TailFiles.ToList());
                if (x.TimberWinR.Inputs.Tcps != null)
                    c._tcps.AddRange(x.TimberWinR.Inputs.Tcps.ToList());
                if (x.TimberWinR.Inputs.Udps != null)
                    c._udps.AddRange(x.TimberWinR.Inputs.Udps.ToList());
            }

            if (x.TimberWinR.Outputs != null)
            {
                if (x.TimberWinR.Outputs.Redis != null)
                    c._redisOutputs.AddRange(x.TimberWinR.Outputs.Redis.ToList());
                if (x.TimberWinR.Outputs.Elasticsearch != null)
                    c._elasticsearchOutputs.AddRange(x.TimberWinR.Outputs.Elasticsearch.ToList());
                if (x.TimberWinR.Outputs.Stdout != null)
                    c._stdoutOutputs.AddRange(x.TimberWinR.Outputs.Stdout.ToList());
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
            _iisw3clogs = new List<IISW3CLog>();
            _logs = new List<Log>();
            _redisOutputs = new List<RedisOutput>();
            _elasticsearchOutputs = new List<ElasticsearchOutput>();
            _stdoutOutputs = new List<StdoutOutput>();
            _tcps = new List<Tcp>();
            _udps = new List<Udp>();
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
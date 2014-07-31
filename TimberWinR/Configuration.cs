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


        private List<Tcp> _tcps = new List<Tcp>();
        public IEnumerable<Tcp> Tcps
        {
            get { return _tcps; }
        }       

        private List<Log> _logs = new List<Log>();
        public IEnumerable<Log> Logs
        {
            get { return _logs; }
        }       

        private List<IISW3CLog> _iisw3clogs = new List<IISW3CLog>();

        public IEnumerable<IISW3CLog> IISW3C
        {
            get { return _iisw3clogs; }
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

        public static Configuration FromFile(string jsonConfFile)
        {
            Configuration c = new Configuration();

            if (!string.IsNullOrEmpty(jsonConfFile))
            {
                string json = File.ReadAllText(jsonConfFile);

                return FromString(json);
            }

            return null;
        }

        public static Configuration FromString(string json)
        {
            Configuration c = new Configuration();

            JsonSerializer serializer = new JsonSerializer();
            TextReader re = new StringReader(json);
            JsonTextReader reader = new JsonTextReader(re);

            var x = serializer.Deserialize<TimberWinR.Parser.RootObject>(reader);

            if (x.TimberWinR.Inputs != null)
            {
                if (x.TimberWinR.Inputs.WindowsEvents != null)
                    c._events = x.TimberWinR.Inputs.WindowsEvents.ToList();
                if (x.TimberWinR.Inputs.IISW3CLogs != null)
                    c._iisw3clogs = x.TimberWinR.Inputs.IISW3CLogs.ToList();
                if (x.TimberWinR.Inputs.Stdins != null)
                    c._stdins = x.TimberWinR.Inputs.Stdins.ToList();
                if (x.TimberWinR.Inputs.Logs != null)
                    c._logs = x.TimberWinR.Inputs.Logs.ToList();
                if (x.TimberWinR.Inputs.Tcps != null)
                    c._tcps = x.TimberWinR.Inputs.Tcps.ToList();
            }

            if (x.TimberWinR.Outputs != null)           
                c._redisOutputs = x.TimberWinR.Outputs.Redis.ToList();          

            if (x.TimberWinR.Filters != null)
                c._filters = x.TimberWinR.AllFilters.ToList();

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
            _tcps = new List<Tcp>();
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
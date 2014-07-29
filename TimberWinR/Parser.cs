using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Reflection;
using System.Runtime.Remoting.Channels;
using System.Text;
using Microsoft.CSharp;
using Microsoft.SqlServer.Server;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NLog;
using TimberWinR.Outputs;


namespace TimberWinR.Parser
{
    interface IValidateSchema
    {
        void Validate();
    }

    public abstract class LogstashFilter : IValidateSchema
    {
        public abstract bool Apply(JObject json);
       
        protected void RenameProperty(JObject json, string oldName, string newName)
        {
            JToken token = json[oldName];
            if (token != null)
            {
                json.Remove(oldName);
                json.Add(newName, token);
            }
        }

        protected bool EvaluateCondition(JObject json, string condition)
        {
            // Create a new instance of the C# compiler
            var cond = condition;

            IList<string> keys = json.Properties().Select(p => p.Name).ToList();
            foreach (string key in keys)
                cond = cond.Replace(string.Format("[{0}]", key), string.Format("\"{0}\"", json[key].ToString()));

            var compiler = new CSharpCodeProvider();

            // Create some parameters for the compiler
            var parms = new System.CodeDom.Compiler.CompilerParameters
            {
                GenerateExecutable = false,
                GenerateInMemory = true
            };
            parms.ReferencedAssemblies.Add("System.dll");
            var code = string.Format(@" using System;
                                        class EvaluatorClass
                                        {{
                                            public bool Evaluate()
                                            {{
                                                return {0};
                                            }}               
                                        }}", cond);

            // Try to compile the string into an assembly
            var results = compiler.CompileAssemblyFromSource(parms, new string[] { code });

            // If there weren't any errors get an instance of "MyClass" and invoke
            // the "Message" method on it
            if (results.Errors.Count == 0)
            {
                var evClass = results.CompiledAssembly.CreateInstance("EvaluatorClass");
                var result = evClass.GetType().
                    GetMethod("Evaluate").
                    Invoke(evClass, null);
                return bool.Parse(result.ToString());
            }
            else
            {
                foreach (var e in results.Errors)
                {
                    LogManager.GetCurrentClassLogger().Error(e);
                    LogManager.GetCurrentClassLogger().Error("Bad Code: {0}", code);
                }
            }

            return false;
        }
        protected void RemoveProperties(JToken token, string[] fields)
        {
            JContainer container = token as JContainer;
            if (container == null) return;

            List<JToken> removeList = new List<JToken>();
            foreach (JToken el in container.Children())
            {
                JProperty p = el as JProperty;
                if (p != null && fields.Contains(p.Name))
                {
                    removeList.Add(el);
                }
                RemoveProperties(el, fields);
            }

            foreach (JToken el in removeList)
            {
                el.Remove();
            }
        }

        protected void ReplaceProperty(JObject json, string propertyName, string propertyValue)
        {
            if (json[propertyName] != null)
                json[propertyName] = propertyValue;
        }

        protected void AddOrModify(JObject json, string fieldName, string fieldValue)
        {
            if (json[fieldName] == null)
                json.Add(fieldName, fieldValue);
            else
                json[fieldName] = fieldValue;
        }

        protected void AddOrModify(JObject json, string fieldName, JToken token)
        {
            if (json[fieldName] == null)
                json.Add(fieldName, token);
            else
                json[fieldName] = token;
        }


        protected string ExpandField(string fieldName, JObject json)
        {
            foreach (var token in json.Children())
            {
                string replaceString = "%{" + token.Path + "}";
                fieldName = fieldName.Replace(replaceString, json[token.Path].ToString());
            }
            return fieldName;
        }

        public abstract void Validate();
   
    }

    [JsonObject(MemberSerialization.OptIn)]
    public class Field
    {
        [JsonProperty(PropertyName = "name")]
        public string Name { get; set; }
        [JsonProperty(PropertyName = "to")]
        public string To { get; set; }
        [JsonProperty(PropertyName = "type")]
        public string FieldType { get; set; }
        

        public Type DataType
        {
            get { return Type.GetType(FieldType); }
        }

        public Field()
        {
            FieldType = "string";
        }

        public Field(string name)
        {
            Name = name;
            To = Name;
            FieldType = "string";
        }
        public Field(string name, string type)
        {
            Name = name;
            if (type.ToLower() == "string")
                type = "System.String";
            else if (type.ToLower() == "datetime")
                type = "System.DateTime";
            else if (type.ToLower() == "int" || type.ToLower() == "integer")
                type = "System.Int32";
            FieldType = type;
            To = Name;
        }
        public Field(string name, string type, string to)
        {
            Name = name;
            FieldType = type;
            To = to;
        }
    }
    
    public class WindowsEvent : IValidateSchema
    {
        public enum FormatKinds
        {
            PRINT, ASC, HEX
        };

        public enum MessageErrorModes
        {
            MSG,
            ERROR,
            NULL
        };

        public enum DirectionKinds
        {
            FW,
            BW
        };

        [JsonProperty(PropertyName = "source")]
        public string Source { get; set; }
        [JsonProperty(PropertyName = "binaryFormat")]
        public FormatKinds BinaryFormat { get; set; }
        [JsonProperty(PropertyName = "msgErrorMode")]
        public MessageErrorModes MsgErrorMode { get; set; }
        [JsonProperty(PropertyName = "direction")]
        public DirectionKinds Direction { get; set; }
        [JsonProperty(PropertyName = "stringsSep")]
        public string StringsSep { get; set; }
        [JsonProperty(PropertyName = "fullEventCode")]
        public bool FullEventCode { get; set; }
        [JsonProperty(PropertyName = "fullText")]
        public bool FullText { get; set; }
        [JsonProperty(PropertyName = "resolveSIDS")]
        public bool ResolveSIDS { get; set; }
        [JsonProperty(PropertyName = "fields")]
        public List<Field> Fields { get; set; }
        [JsonProperty(PropertyName = "formatMsg")]
        public bool FormatMsg { get; set; }

        public WindowsEvent()
        {
            Source = "System";
            StringsSep = "|";
            FormatMsg = true;
            FullText = true;
            BinaryFormat = FormatKinds.ASC;
            FullEventCode = false;
          
            Fields = new List<Field>();
            Fields.Add(new Field("EventLog", "string"));
            Fields.Add(new Field("RecordNumber", "int"));
            Fields.Add(new Field("TimeGenerated", "DateTime"));
            Fields.Add(new Field("TimeWritten", "DateTime"));
            Fields.Add(new Field("EventID", "int"));
            Fields.Add(new Field("EventType", "int"));
            Fields.Add(new Field("EventTypeName", "string"));
            Fields.Add(new Field("EventCategory", "int"));
            Fields.Add(new Field("EventCategoryName", "string"));
            Fields.Add(new Field("SourceName", "string"));
            Fields.Add(new Field("Strings", "string"));
            Fields.Add(new Field("ComputerName", "string"));
            Fields.Add(new Field("SID", "string"));
            Fields.Add(new Field("Message", "string"));
            Fields.Add(new Field("Data", "string"));              
        }

        public void Validate()
        {

        }
    }
   
    public class Log : IValidateSchema
    {
        [JsonProperty(PropertyName = "location")]
        public string Location { get; set; }
        [JsonProperty(PropertyName = "iCodepage")]
        public int CodePage { get; set; }
        [JsonProperty(PropertyName = "recurse")]
        public int Recurse { get; set; }
        [JsonProperty(PropertyName = "splitLongLines")]
        public bool SplitLongLines { get; set; }

        [JsonProperty(PropertyName = "fields")]
        public List<Field> Fields { get; set; }

        public Log()
        {
            Fields = new List<Field>();
            Fields.Add(new Field("LogFilename", "string"));
            Fields.Add(new Field("Index", "integer"));
            Fields.Add(new Field("Text", "string"));
        }

        public void Validate()
        {
            
        }
    }

    public class Tcp : IValidateSchema
    {
        [JsonProperty(PropertyName = "port")]
        public int Port { get; set; }

        public Tcp()
        {
            Port = 5140;
        }

        public void Validate()
        {
            
        }
    }
    
    public class IISW3CLog : IValidateSchema
    {
        [JsonProperty(PropertyName = "name")]
        public string Name { get; set; }
        [JsonProperty(PropertyName = "location")]
        public string Location { get; set; }
        [JsonProperty(PropertyName = "iCodepage")]
        public int CodePage { get; set; }
        [JsonProperty(PropertyName = "recurse")]
        public int Recurse { get; set; }
        [JsonProperty(PropertyName = "dQuotes")]
        public bool DoubleQuotes { get; set; }
        [JsonProperty(PropertyName = "dirTime")]
        public bool DirTime { get; private set; }
        [JsonProperty(PropertyName = "consolidateLogs")]
        public bool ConsolidateLogs { get; private set; }
        [JsonProperty(PropertyName = "minDateMod")]
        public DateTime? MinDateMod { get; private set; }

        [JsonProperty(PropertyName = "fields")]
        public List<Field> Fields { get; set; }

        public IISW3CLog()
        {
            CodePage = -2;
            Recurse = 0;
            Fields = new List<Field>();

            Fields.Add(new Field("LogFilename", "string"));
            Fields.Add(new Field("LogRow", "integer" ));
            Fields.Add(new Field("date", "DateTime" ));
            Fields.Add(new Field("time", "DateTime" ));
            Fields.Add(new Field("c-ip", "string" ));
            Fields.Add(new Field("cs-username", "string" ));
            Fields.Add(new Field("s-sitename", "string" ));
            Fields.Add(new Field("s-computername", "integer" ));
            Fields.Add(new Field("s-ip", "string" ));
            Fields.Add(new Field("s-port", "integer" ));
            Fields.Add(new Field("cs-method", "string" ));
            Fields.Add(new Field("cs-uri-stem", "string" ));
            Fields.Add(new Field("cs-uri-query", "string" ));
            Fields.Add(new Field("sc-status", "integer" ));
            Fields.Add(new Field("sc-substatus", "integer" ));
            Fields.Add(new Field("sc-win32-status", "integer" ));
            Fields.Add(new Field("sc-bytes", "integer" ));
            Fields.Add(new Field("cs-bytes", "integer" ));
            Fields.Add(new Field("time-taken", "integer" ));
            Fields.Add(new Field("cs-version", "string" ));
            Fields.Add(new Field("cs-host", "string" ));
            Fields.Add(new Field("cs(User-Agent)", "string" ));
            Fields.Add(new Field("cs(Cookie)", "string" ));
            Fields.Add(new Field("cs(Referer)", "string" ));
            Fields.Add(new Field("s-event", "string" ));
            Fields.Add(new Field("s-process-type", "string" ));
            Fields.Add(new Field("s-user-time", "double" ));
            Fields.Add(new Field("s-kernel-time", "double" ));
            Fields.Add(new Field("s-page-faults", "integer" ));
            Fields.Add(new Field("s-total-procs", "integer" ));
            Fields.Add(new Field("s-active-procs", "integer" ));
            Fields.Add(new Field("s-stopped-procs", "integer"));
        }

        public void Validate()
        {
          
        }
    }

    public partial class RedisOutput
    {      
        [JsonProperty(PropertyName = "host")]
        public string[] Host { get; set; }
        [JsonProperty(PropertyName = "index")]
        public string Index { get; set; }
        [JsonProperty(PropertyName = "port")]
        public int Port { get; set; }
        [JsonProperty(PropertyName = "timeout")]
        public int Timeout { get; set; }
        [JsonProperty(PropertyName = "batch_count")]
        public int BatchCount { get; set; }
        [JsonProperty(PropertyName = "threads")]
        public int NumThreads { get; set; }
        [JsonProperty(PropertyName = "interval")]
        public int Interval { get; set; }

        public RedisOutput()
        {
            Port = 6379;
            Index = "logstash";
            Host = new string[] {"localhost"};
            Timeout = 10000;
            BatchCount = 10;
            NumThreads = 1;
            Interval = 5000;
        }
    }
  
    public class OutputTargets
    {
        [JsonProperty("Redis")]
        public RedisOutput[] Redis { get; set; }
    }

    public class InputSources
    {
        [JsonProperty("WindowsEvents")]
        public WindowsEvent[] WindowsEvents { get; set; }

        [JsonProperty("Logs")]
        public Log[] Logs { get; set; }

        [JsonProperty("Tcp")]
        public Tcp[] Tcps { get; set; }

        [JsonProperty("IISW3CLogs")]
        public IISW3CLog[] IISW3CLogs { get; set; }
    }
         
    public partial class Grok : LogstashFilter, IValidateSchema
    {
        public class GrokFilterException : Exception
        {
            public GrokFilterException()
                : base("Grok filter missing required match, must be 2 array entries.")                   
            {
            }
        }

        public class GrokAddTagException : Exception
        {
            public GrokAddTagException()
                : base("Grok filter add_tag requires tuples")
            {
            }
        }
        [JsonProperty("condition")]
        public string Condition { get; set; }

        [JsonProperty("drop_if_match")]
        public bool DropIfMatch { get; set; }

        [JsonProperty("match")]
        public string[] Match { get; set; }

        [JsonProperty("add_tag")]
        public string[] AddTag { get; set; }

        [JsonProperty("add_field")]
        public string[] AddField { get; set; }        
         
        [JsonProperty("remove_field")]
        public string[] RemoveField { get; set; }    

        [JsonProperty("remove_tag")]
        public string[] RemoveTag { get; set; }

        public override void Validate()
        {
            if (Match == null || Match.Length != 2)
                throw new GrokFilterException();

            if (AddTag != null && AddTag.Length%2 != 0)
                throw new GrokAddTagException();
        }
    }

    public partial class DateFilter : LogstashFilter
    {
        public class DateFilterMatchException : Exception
        {
            public DateFilterMatchException()
                : base("Date filter missing required match, must be 2 array entries.")
            {
            }
        }

        public class DateFilterTargetException : Exception
        {
            public DateFilterTargetException()
                : base("Date filter missing target")
            {
            }
        }

        [JsonProperty("condition")]
        public string Condition { get; set; }

        [JsonProperty("locale")]
        public string Locale { get; set; }

        [JsonProperty("match")]
        public string[] Match { get; set; }

        [JsonProperty("target")]
        public string Target { get; set; }

        [JsonProperty("convertToUTC")]
        public bool ConvertToUTC { get; set; }

        [JsonProperty("pattern")]
        public string[] Patterns { get; set; }

        [JsonProperty("add_field")]
        public string[] AddField { get; set; }           

        public override void Validate()
        {
            if (Match == null || Match.Length < 2)
                throw new DateFilterMatchException();

            if (string.IsNullOrEmpty(Target))
                throw new DateFilterTargetException();
        }

        public DateFilter()
        {
            Target = "timestamp";
            Locale = "en-US";
        }
    }

    public partial class Mutate : LogstashFilter
    {
        [JsonProperty("condition")]
        public string Condition { get; set; }

        [JsonProperty("rename")]
        public string[] Rename { get; set; }

        [JsonProperty("replace")]
        public string[] Replace { get; set; }

        [JsonProperty("split")]
        public string[] Split { get; set; }

        public override void Validate()
        {
           
        }
    }
    
    public class Filter
    {
        [JsonProperty("grok")]
        public Grok Grok { get; set; }

        [JsonProperty("mutate")]
        public Mutate Mutate { get; set; }

        [JsonProperty("date")]
        public DateFilter Date { get; set; }
    }
   
    public class TimberWinR
    {
        [JsonProperty("Inputs")]
        public InputSources Inputs { get; set; }
        [JsonProperty("Filters")]
        public List<Filter> Filters { get; set; }
        [JsonProperty("Outputs")]
        public OutputTargets Outputs { get; set;  }

        public LogstashFilter[] AllFilters
        {
            get
            {
                var list = new List<LogstashFilter>();
                foreach (var filter in Filters)
                {
                    foreach (var prop in filter.GetType().GetProperties())
                    {
                        object typedFilter = filter.GetType().GetProperty(prop.Name).GetValue(filter, null);
                        if (typedFilter != null && typedFilter is LogstashFilter)
                        {
                            list.Add(typedFilter as LogstashFilter);
                        }
                    }
                }
                return list.ToArray();
            }
        }
    }
   
    public class RootObject
    {
        public TimberWinR TimberWinR { get; set; }
    }
}

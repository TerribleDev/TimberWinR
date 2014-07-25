using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using Microsoft.SqlServer.Server;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NLog;

namespace TimberWinR.Parser
{
    public abstract class LogstashFilter
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

        protected string ExpandField(string fieldName, JObject json)
        {
            foreach (var token in json.Children())
            {
                string replaceString = "%{" + token.Path + "}";
                fieldName = fieldName.Replace(replaceString, json[token.Path].ToString());
            }
            return fieldName;
        }
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
    
    public class WindowsEvent
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
            StringsSep = "|";
            FormatMsg = true;
            FullText = true;
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
    }
   
    public class Log
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
    }
    
    public class IISW3CLog
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
    }

    public class InputSources
    {
        [JsonProperty("WindowsEvents")]
        public WindowsEvent[] WindowsEvents { get; set; }

        [JsonProperty("Logs")]
        public Log[] Logs { get; set; }

        [JsonProperty("IISW3CLogs")]
        public IISW3CLog[] IISW3CLogs { get; set; }
    }
         
    public partial class Grok : LogstashFilter
    {
        [JsonProperty("condition")]
        public string Condition { get; set; }

        [JsonProperty("match")]
        public string[] Match { get; set; }

        [JsonProperty("add_tag")]
        public string[] AddTag { get; set; }

        [JsonProperty("add_field")]
        public string[] AddField { get; set; }      
    }

    public class Date : LogstashFilter
    {
        public string field { get; set; }
        public string target { get; set; }
        public bool convertToUTC { get; set; }
        public List<string> Pattern { get; set; }

        public override bool Apply(JObject json)
        {
            return false;
        }
    }

    public partial class Mutate : LogstashFilter
    {
        [JsonProperty("rename")]
        public string[] Rename { get; set; }

        [JsonProperty("replace")]
        public string[] Replace { get; set; }

        [JsonProperty("split")]
        public string[] Split { get; set; }       
    }
    
    public class Filter
    {
        [JsonProperty("grok")]
        public Grok Grok { get; set; }

        [JsonProperty("mutate")]
        public Mutate Mutate { get; set; }
    }
   
    public class TimberWinR
    {
        [JsonProperty("Inputs")]
        public InputSources Inputs { get; set; }
        public List<Filter> Filters { get; set; }
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

using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Linq.Expressions;
using System.Linq.Dynamic;
using System.Reflection;
using System.Runtime.Remoting.Channels;
using System.Text;
using Microsoft.CSharp;
using Microsoft.SqlServer.Server;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NLog;
using NLog.Config;
using TimberWinR.Outputs;
using System.CodeDom.Compiler;

namespace TimberWinR.Parser
{
    using System.Text.RegularExpressions;

    interface IValidateSchema
    {
        void Validate();
    }


    public abstract class LogstashFilter : IValidateSchema
    {
        public abstract bool Apply(JObject json);

        protected void RemoveProperty(JObject json, string name)
        {
            JToken token = json[name];
            if (token != null)
            {
                json.Remove(name);
            }
        }

        protected void RenameProperty(JObject json, string oldName, string newName)
        {
            JToken token = json[oldName];
            if (token != null)
            {
                json.Add(newName, token.DeepClone());
                json.Remove(oldName);
            }
        }

        public abstract JObject ToJson();

        protected bool EvaluateCondition(JObject json, string condition)
        {
            var cond = condition;

            IList<string> keys = json.Properties().Select(pn => pn.Name).ToList();
            foreach (string key in keys)
                cond = cond.Replace(string.Format("[{0}]", key), string.Format("{0}", json[key].ToString()));

            var p = Expression.Parameter(typeof(JObject), "");
            var e = System.Linq.Dynamic.DynamicExpression.ParseLambda(new[] { p }, null, cond);

            var result = e.Compile().DynamicInvoke(json);

            return (bool)result;
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
        [JsonProperty(PropertyName = "interval")]
        public int Interval { get; set; }

        public WindowsEvent()
        {
            Interval = 60; // Every minute
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

    public class Stdin : IValidateSchema
    {
        [JsonProperty(PropertyName = "codec")]
        public CodecArguments CodecArguments { get; set; }

        public void Validate()
        {

        }
    }

    public class GeneratorParameters : IValidateSchema
    {
        [JsonProperty(PropertyName = "type")]
        public string Type { get; set; }

        [JsonProperty(PropertyName = "codec")]
        public CodecArguments CodecArguments { get; set; }

        [JsonProperty(PropertyName = "message")]
        public string Message { get; set; }

        [JsonProperty(PropertyName = "count")]
        public int Count { get; set; }

        [JsonProperty(PropertyName = "rate")]
        public int Rate { get; set; }

        public void Validate()
        {
        }

        public GeneratorParameters()
        {
            Count = 0; // Infinity messages
            Rate = 10; // Milliseconds
            Message = "Hello, world!";
            CodecArguments = new CodecArguments();
            CodecArguments.Type = CodecArguments.CodecType.plain;
        }
    }


    public class CodecArguments
    {
        public enum CodecType
        {
            singleline,
            multiline,
            json,
            plain
        };

        public enum WhatType
        {
            previous,
            next
        };

        [JsonProperty(PropertyName = "type")]
        public CodecType Type { get; set; }
        [JsonProperty(PropertyName = "pattern")]
        public string Pattern { get; set; }
        [JsonProperty(PropertyName = "what")]
        public WhatType What { get; set; }
        [JsonProperty(PropertyName = "negate")]
        public bool Negate { get; set; }
        [JsonProperty(PropertyName = "multiline_tag")]
        public string MultilineTag { get; set; }

        public Regex Re { get; set; }

        public CodecArguments()
        {
            Negate = false;
            MultilineTag = "multiline";
        }
    }

    public class TailFileArguments : IValidateSchema
    {
        [JsonProperty(PropertyName = "type")]
        public string Type { get; set; }
        [JsonProperty(PropertyName = "location")]
        public string Location { get; set; }
        [JsonProperty(PropertyName = "recurse")]
        public int Recurse { get; set; }
        [JsonProperty(PropertyName = "fields")]
        public List<Field> Fields { get; set; }
        [JsonProperty(PropertyName = "interval")]
        public int Interval { get; set; }
        [JsonProperty(PropertyName = "logSource")]
        public string LogSource { get; set; }
        [JsonProperty(PropertyName = "codec")]
        public CodecArguments CodecArguments { get; set; }

        public TailFileArguments()
        {
            Fields = new List<Field>();
            Fields.Add(new Field("LogFilename", "string"));
            Fields.Add(new Field("Index", "integer"));
            Fields.Add(new Field("Text", "string"));
            Interval = 30;
        }

        public void Validate()
        {

        }
    }

    public class LogParameters : IValidateSchema
    {
        [JsonProperty(PropertyName = "type")]
        public string Type { get; set; }
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
        [JsonProperty(PropertyName = "interval")]
        public int Interval { get; set; }
        [JsonProperty(PropertyName = "logSource")]
        public string LogSource { get; set; }
        [JsonProperty(PropertyName = "codec")]
        public CodecArguments CodecArguments { get; set; }

        public LogParameters()
        {
            Fields = new List<Field>();
            Fields.Add(new Field("LogFilename", "string"));
            Fields.Add(new Field("Index", "integer"));
            Fields.Add(new Field("Text", "string"));
            Interval = 30;
        }

        public void Validate()
        {

        }
    }

    public class TcpParameters : IValidateSchema
    {
        [JsonProperty(PropertyName = "port")]
        public int Port { get; set; }
        [JsonProperty(PropertyName = "type")]
        public string Type { get; set; }
        [JsonProperty("add_field")]
        public string[] AddFields { get; set; }
        [JsonProperty("rename")]
        public string[] Renames { get; set; }

        public TcpParameters()
        {
            Port = 5140;
            Type = "Win32-Tcp";
        }

        public void Validate()
        {
        }
    }


    public class UdpParameters : IValidateSchema
    {
        [JsonProperty(PropertyName = "port")]
        public int Port { get; set; }
        [JsonProperty(PropertyName = "type")]
        public string Type { get; set; }
        [JsonProperty("add_field")]
        public string[] AddFields { get; set; }
        [JsonProperty("rename")]
        public string[] Renames { get; set; }

        public UdpParameters()
        {
            Port = 5142;
            Type = "Win32-Udp";
        }

        public void Validate()
        {
        }
    }
    public class W3CLogParameters : IValidateSchema
    {
        [JsonProperty(PropertyName = "location")]
        public string Location { get; set; }
        [JsonProperty(PropertyName = "separator")]
        public string Separator { get; set; }
        [JsonProperty(PropertyName = "iCodepage")]
        public int CodePage { get; set; }
        [JsonProperty(PropertyName = "dtLines")]
        public int DtLines { get; set; }
        [JsonProperty(PropertyName = "dQuotes")]
        public bool DoubleQuotes { get; set; }


        [JsonProperty(PropertyName = "fields")]
        public List<Field> Fields { get; set; }

        public W3CLogParameters()
        {
            CodePage = 0;
            DtLines = 10;
            Fields = new List<Field>();
            Separator = "auto";

            Fields.Add(new Field("LogFilename", "string"));
            Fields.Add(new Field("RowNumber", "integer"));
        }

        public void Validate()
        {

        }
    }


    public class IISW3CLogParameters : IValidateSchema
    {
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

        public IISW3CLogParameters()
        {
            CodePage = -2;
            Recurse = 0;
            Fields = new List<Field>();

            Fields.Add(new Field("LogFilename", "string"));
            Fields.Add(new Field("LogRow", "integer"));
            Fields.Add(new Field("date", "DateTime"));
            Fields.Add(new Field("time", "DateTime"));
            Fields.Add(new Field("c-ip", "string"));
            Fields.Add(new Field("cs-username", "string"));
            Fields.Add(new Field("s-sitename", "string"));
            Fields.Add(new Field("s-computername", "integer"));
            Fields.Add(new Field("s-ip", "string"));
            Fields.Add(new Field("s-port", "integer"));
            Fields.Add(new Field("cs-method", "string"));
            Fields.Add(new Field("cs-uri-stem", "string"));
            Fields.Add(new Field("cs-uri-query", "string"));
            Fields.Add(new Field("sc-status", "integer"));
            Fields.Add(new Field("sc-substatus", "integer"));
            Fields.Add(new Field("sc-win32-status", "integer"));
            Fields.Add(new Field("sc-bytes", "integer"));
            Fields.Add(new Field("cs-bytes", "integer"));
            Fields.Add(new Field("time-taken", "integer"));
            Fields.Add(new Field("cs-version", "string"));
            Fields.Add(new Field("cs-host", "string"));
            Fields.Add(new Field("cs(User-Agent)", "string"));
            Fields.Add(new Field("cs(Cookie)", "string"));
            Fields.Add(new Field("cs(Referer)", "string"));
            Fields.Add(new Field("s-event", "string"));
            Fields.Add(new Field("s-process-type", "string"));
            Fields.Add(new Field("s-user-time", "double"));
            Fields.Add(new Field("s-kernel-time", "double"));
            Fields.Add(new Field("s-page-faults", "integer"));
            Fields.Add(new Field("s-total-procs", "integer"));
            Fields.Add(new Field("s-active-procs", "integer"));
            Fields.Add(new Field("s-stopped-procs", "integer"));
        }

        public void Validate()
        {

        }
    }

    public class StatsDOutputParameters : IValidateSchema
    {
        public class StatsDGaugeHashException : Exception
        {
            public StatsDGaugeHashException()
                : base("StatsD output 'gauge' must be an array of pairs.")
            {
            }
        }

        public class StatsDCountHashException : Exception
        {
            public StatsDCountHashException()
                : base("StatsD output 'count' must be an array of pairs.")
            {
            }
        }
        [JsonProperty(PropertyName = "type")]
        public string InputType { get; set; }
        [JsonProperty(PropertyName = "sender")]
        public string Sender { get; set; }
        [JsonProperty(PropertyName = "namespace")]
        public string Namespace { get; set; }    
        [JsonProperty(PropertyName = "host")]
        public string Host { get; set; }
        [JsonProperty(PropertyName = "port")]
        public int Port { get; set; }
        [JsonProperty(PropertyName = "interval")]
        public int Interval { get; set; }
        [JsonProperty(PropertyName = "flush_size")]
        public int FlushSize { get; set; }
        [JsonProperty(PropertyName = "idle_flush_time")]
        public int IdleFlushTimeInSeconds { get; set; }
        [JsonProperty(PropertyName = "max_queue_size")]
        public int MaxQueueSize { get; set; }
        [JsonProperty(PropertyName = "queue_overflow_discard_oldest")]
        public bool QueueOverflowDiscardOldest { get; set; }
        [JsonProperty(PropertyName = "threads")]
        public int NumThreads { get; set; }
        [JsonProperty(PropertyName = "sample_rate")]
        public double SampleRate { get; set; }
        [JsonProperty(PropertyName = "increment")] // Array: metric names
        public string[] Increments { get; set; }
        [JsonProperty(PropertyName = "decrement")] // Array: metric names
        public string[] Decrements { get; set; }
        [JsonProperty(PropertyName = "gauge")] // Hash: metric_name => gauge
        public string[] Gauges { get; set; }
        [JsonProperty(PropertyName = "count")] // Hash: metric_name => count
        public string[] Counts { get; set; }
        [JsonProperty(PropertyName = "timing")] // Hash: metric_name => count
        public string[] Timings { get; set; }

        public StatsDOutputParameters()
        {
            SampleRate = 1;
            Port = 8125;
            Host = "localhost";
            Interval = 5000;
            FlushSize = 5000;
            IdleFlushTimeInSeconds = 10;
            QueueOverflowDiscardOldest = true;
            MaxQueueSize = 50000;
            NumThreads = 1;
            Namespace = "timberwinr";
            Sender = System.Environment.MachineName.ToLower() + "." +
                     Microsoft.Win32.Registry.LocalMachine.OpenSubKey(
                         @"SYSTEM\CurrentControlSet\services\Tcpip\Parameters")
                         .GetValue("Domain", "")
                         .ToString().ToLower();
        }

        public void Validate()
        {
            if (Gauges != null && Gauges.Length % 2 != 0)
                throw new StatsDGaugeHashException();

            if (Counts != null && Counts.Length % 2 != 0)
                throw new StatsDCountHashException();
        }
    }


    public class ElasticsearchOutputParameters
    {
        const string IndexDatePattern = "(%\\{(?<format>[^\\}]+)\\})";

        [JsonProperty(PropertyName = "host")]
        public string[] Host { get; set; }
        [JsonProperty(PropertyName = "index")]
        public string Index { get; set; }
        [JsonProperty(PropertyName = "port")]
        public int Port { get; set; }
        [JsonProperty(PropertyName = "timeout")]
        public int Timeout { get; set; }
        [JsonProperty(PropertyName = "threads")]
        public int NumThreads { get; set; }
        [JsonProperty(PropertyName = "protocol")]
        public string Protocol { get; set; }
        [JsonProperty(PropertyName = "interval")]
        public int Interval { get; set; }
        [JsonProperty(PropertyName = "flush_size")]
        public int FlushSize { get; set; }
        [JsonProperty(PropertyName = "idle_flush_time")]
        public int IdleFlushTimeInSeconds { get; set; }
        [JsonProperty(PropertyName = "max_queue_size")]
        public int MaxQueueSize { get; set; }
        [JsonProperty(PropertyName = "queue_overflow_discard_oldest")]
        public bool QueueOverflowDiscardOldest { get; set; }
        [JsonProperty(PropertyName = "enable_ping")]
        public bool EnablePing { get; set; }
        [JsonProperty(PropertyName = "ping_timeout")]
        public int PingTimeout { get; set; }

        public ElasticsearchOutputParameters()
        {
            FlushSize = 5000;
            IdleFlushTimeInSeconds = 10;
            Protocol = "http";
            Port = 9200;
            Index = "";
            Host = new string[] { "localhost" };
            Timeout = 10000;
            NumThreads = 1;
            Interval = 1000;
            QueueOverflowDiscardOldest = true;
            MaxQueueSize = 50000;
            EnablePing = false;
            PingTimeout = 0;
        }

        public string GetIndexName(JObject json)
        {
            ////check if the submitted JSON object provides a custom index. If yes, use this one
            var token = json["_index"];
            var indexName = token == null ? this.Index : token.Value<string>();

            if (string.IsNullOrEmpty(indexName))
            {
                indexName = string.Format("logstash-{0}", DateTime.UtcNow.ToString("yyyy.MM.dd"));
            }
            else
            {
                var date = DateTime.UtcNow;
                if (json["@timestamp"] != null)
                {
                    date = DateTime.Parse(json["@timestamp"].ToString());
                }

                var match = Regex.Match(indexName, IndexDatePattern);
                if (match.Success)
                {
                    indexName = Regex.Replace(indexName, IndexDatePattern, date.ToString(match.Groups["format"].Value));
                }
            }

            return indexName;
        }

        public string GetTypeName(JObject json)
        {
            string typeName = "Win32-Elasticsearch";
            if (json["type"] != null)
            {
                typeName = json["type"].ToString();
            }
            return typeName;
        }

    }

    public class RedisOutputParameters
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
        [JsonProperty(PropertyName = "max_batch_count")]
        public int MaxBatchCount { get; set; }
        [JsonProperty(PropertyName = "threads")]
        public int NumThreads { get; set; }
        [JsonProperty(PropertyName = "interval")]
        public int Interval { get; set; }
        [JsonProperty(PropertyName = "max_queue_size")]
        public int MaxQueueSize { get; set; }
        [JsonProperty(PropertyName = "queue_overflow_discard_oldest")]
        public bool QueueOverflowDiscardOldest { get; set; }

        public RedisOutputParameters()
        {
            Port = 6379;
            Index = "logstash";
            Host = new string[] { "localhost" };
            Timeout = 10000;
            BatchCount = 200;
            NumThreads = 1;
            Interval = 5000;
            QueueOverflowDiscardOldest = true;
            MaxQueueSize = 50000;
        }
    }



    public class StdoutOutputParameters
    {
        [JsonProperty(PropertyName = "interval")]
        public int Interval { get; set; }

        public StdoutOutputParameters()
        {
            Interval = 1000;
        }
    }

    public class FileOutputParameters
    {
        public enum FormatKind
        {
            none, indented
        };

        [JsonProperty(PropertyName = "interval")]
        public int Interval { get; set; }

        [JsonProperty(PropertyName = "file_name")]
        public string FileName { get; set; }

        [JsonProperty(PropertyName = "format")]
        public FormatKind Format { get; set; }

        public FileOutputParameters()
        {
            Format = FormatKind.none;
            Interval = 1000;
            FileName = "timberwinr.out";
        }

        public Newtonsoft.Json.Formatting ToFormat()
        {
            switch (Format)
            {
                case FormatKind.indented:
                    return Newtonsoft.Json.Formatting.Indented;

                case FormatKind.none:
                default:
                    return Newtonsoft.Json.Formatting.None;
            }
        }
    }

    public class OutputTargets
    {
        [JsonProperty("Redis")]
        public RedisOutputParameters[] Redis { get; set; }

        [JsonProperty("Elasticsearch")]
        public ElasticsearchOutputParameters[] Elasticsearch { get; set; }

        [JsonProperty("Stdout")]
        public StdoutOutputParameters[] Stdout { get; set; }

        [JsonProperty("File")]
        public FileOutputParameters[] File { get; set; }

        [JsonProperty("StatsD")]
        public StatsDOutputParameters[] StatsD { get; set; }
    }

    public class InputSources
    {
        [JsonProperty("WindowsEvents")]
        public WindowsEvent[] WindowsEvents { get; set; }

        [JsonProperty("Logs")]
        public LogParameters[] Logs { get; set; }

        [JsonProperty("TailFiles")]
        public TailFileArguments[] TailFilesArguments { get; set; }

        [JsonProperty("Tcp")]
        public TcpParameters[] Tcps { get; set; }

        [JsonProperty("Udp")]
        public UdpParameters[] Udps { get; set; }

        [JsonProperty("IISW3CLogs")]
        public IISW3CLogParameters[] IISW3CLogs { get; set; }

        [JsonProperty("W3CLogs")]
        public W3CLogParameters[] W3CLogs { get; set; }

        [JsonProperty("Stdin")]
        public Stdin[] Stdins { get; set; }

        [JsonProperty("Generator")]
        public GeneratorParameters[] Generators { get; set; }
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

        [JsonProperty("type")]
        public string Type { get; set; }

        [JsonProperty("condition")]
        public string Condition { get; set; }

        [JsonProperty("drop")]
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
            if (Match == null || Match.Length % 2 != 0)
                throw new GrokFilterException();

            if (AddTag != null && AddTag.Length % 2 != 0)
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

        [JsonProperty("type")]
        public string Type { get; set; }

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
            Target = "@timestamp";
            Locale = "en-US";
        }
    }

    public partial class Mutate : LogstashFilter
    {
        [JsonProperty("type")]
        public string Type { get; set; }

        [JsonProperty("condition")]
        public string Condition { get; set; }

        [JsonProperty("remove")]
        public string[] Remove { get; set; }

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

    public partial class GeoIP : LogstashFilter
    {
        public class GeoIPMissingSourceException : Exception
        {
            public GeoIPMissingSourceException()
                : base("GeoIP filter source is required")
            {
            }
        }

        public class GeoIPAddFieldException : Exception
        {
            public GeoIPAddFieldException()
                : base("GeoIP filter add_field requires tuples")
            {
            }
        }

        [JsonProperty("type")]
        public string Type { get; set; }

        [JsonProperty("condition")]
        public string Condition { get; set; }

        [JsonProperty("source")]
        public string Source { get; set; }

        [JsonProperty("target")]
        public string Target { get; set; }

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
            if (string.IsNullOrEmpty(Source))
                throw new GeoIPMissingSourceException();

            if (AddField != null && AddField.Length % 2 != 0)
                throw new GeoIPAddFieldException();
        }
    }


    public partial class Json : LogstashFilter
    {
        public class JsonMissingSourceException : Exception
        {
            public JsonMissingSourceException()
                : base("JSON filter source is required")
            {
            }
        }

        public class JsonAddFieldException : Exception
        {
            public JsonAddFieldException()
                : base("JSON filter add_field requires tuples")
            {
            }
        }


        [JsonProperty("type")]
        public string Type { get; set; }

        [JsonProperty("condition")]
        public string Condition { get; set; }

        [JsonProperty("source")]
        public string Source { get; set; }

        [JsonProperty("remove_source")]
        public bool RemoveSource { get; set; }

        [JsonProperty("target")]
        public string Target { get; set; }

        [JsonProperty("add_tag")]
        public string[] AddTag { get; set; }

        [JsonProperty("add_field")]
        public string[] AddField { get; set; }

        [JsonProperty("remove_field")]
        public string[] RemoveField { get; set; }

        [JsonProperty("remove_tag")]
        public string[] RemoveTag { get; set; }

        [JsonProperty("rename")]
        public string[] Rename { get; set; }

        [JsonProperty("promote")]
        public string Promote { get; set; }


        public override void Validate()
        {
            if (string.IsNullOrEmpty(Source))
                throw new JsonMissingSourceException();

            if (AddField != null && AddField.Length % 2 != 0)
                throw new JsonAddFieldException();
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

        [JsonProperty("json")]
        public Json Json { get; set; }

        [JsonProperty("geoip")]
        public GeoIP GeoIP { get; set; }

        [JsonProperty("grokFilters")]
        public Grok[] Groks { get; set; }

        [JsonProperty("mutateFilters")]
        public Mutate[] Mutates { get; set; }

        [JsonProperty("dateFilters")]
        public DateFilter[] Dates { get; set; }

        [JsonProperty("jsonFilters")]
        public Json[] Jsons { get; set; }

        [JsonProperty("geoipFilters")]
        public GeoIP[] GeoIPs { get; set; }
    }

    public class TimberWinR
    {
        [JsonProperty("Inputs")]
        public InputSources Inputs { get; set; }
        [JsonProperty("Filters")]
        public List<Filter> Filters { get; set; }
        [JsonProperty("Outputs")]
        public OutputTargets Outputs { get; set; }

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
                        else if (typedFilter != null && typedFilter.GetType().IsArray && typeof(LogstashFilter).IsAssignableFrom(typedFilter.GetType().GetElementType()))
                        {
                            IEnumerable<LogstashFilter> lf = typedFilter as IEnumerable<LogstashFilter>;
                            list.AddRange(lf);
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

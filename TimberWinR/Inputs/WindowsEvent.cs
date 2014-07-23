using System;
using System.Collections.Generic;
using System.Text;
using System.Xml.Linq;
using Microsoft.SqlServer.Server;

namespace TimberWinR.Inputs
{
    public class WindowsEvent : InputBase
    {
        public const string ParentTagName = "WindowsEvents";
        public const string TagName = "Event";

        public string Source { get; private set; }
        public bool FullText { get; private set; }
        public bool ResolveSIDS { get; private set; }
        public bool FormatMsg { get; private set; }
        public string MsgErrorMode { get; private set; }
        public bool FullEventCode { get; private set; }
        public string Direction { get; private set; }
        public string StringsSep { get; private set; }
        public string ICheckpoint { get; private set; }
        public string BinaryFormat { get; private set; }
        public List<FieldDefinition> Fields { get; private set; }

        public static void Parse(List<WindowsEvent> events, XElement eventElement)
        {
            events.Add(parseEvent(eventElement));
        }

        static WindowsEvent parseEvent(XElement e)
        {
            return new WindowsEvent(e);
        }

        WindowsEvent(XElement parent)
        {
            Source = ParseRequiredStringAttribute(parent, "source");
            FullText = ParseBoolAttribute(parent, "fullText", true);
            ResolveSIDS = ParseBoolAttribute(parent, "resolveSIDS", true);
            FormatMsg = ParseBoolAttribute(parent, "formatMsg", true);
            MsgErrorMode = ParseEnumAttribute(parent, "msgErrorMode", new string[] {"NULL", "ERROR", "MSG"}, "MSG");
            FullEventCode = ParseBoolAttribute(parent, "fullEventCode", false); ;
            Direction = ParseEnumAttribute(parent, "direction", new string[] { "FW", "BW" }, "FW");
            StringsSep = ParseStringAttribute(parent, "stringsSep", "|");
            BinaryFormat = ParseEnumAttribute(parent, "binaryFormat", new string[] { "ASC", "PRINT", "HEX" }, "PRINT");
            ParseFields(parent);
        }
                            
        private void ParseFields(XElement parent)
        {
            Dictionary<string, Type> allPossibleFields = new Dictionary<string, Type>()
            {
                { "EventLog", typeof(string) },
                { "RecordNumber", typeof(int) },
                { "TimeGenerated", typeof(DateTime) },
                { "TimeWritten", typeof(DateTime) },
                { "EventID", typeof(int) },
                { "EventType", typeof(int) },
                { "EventTypeName", typeof(string) },
                { "EventCategory", typeof(int) },
                { "EventCategoryName", typeof(string) },
                { "SourceName", typeof(string) },
                { "Strings", typeof(string) },
                { "ComputerName", typeof(string) },
                { "SID", typeof(string) },
                { "Message", typeof(string) },
                { "Data", typeof(string) }
            };

            Fields = base.parseFields(parent, allPossibleFields);
        }

    }
}
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Xml.Linq;

namespace TimberWinR.Inputs
{
    public class IISLog : InputBase
    {
        public const string ParentTagName = "IISLogs";
        public new const string TagName = "IISLog";

        public string Name { get; private set; }
        public string Location { get; private set; }
        public int ICodepage { get; private set; }
        public int Recurse { get; private set; }
        public string MinDateMod { get; private set; }
        public string Locale { get; private set; }
        public string ICheckpoint { get; private set; }
        public List<FieldDefinition> Fields { get; private set; }

        public static void Parse(List<IISLog> iislogs, XElement iislogElement)
        {
            iislogs.Add(parseIISLog(iislogElement));
        }

        static IISLog parseIISLog(XElement e)
        {
            return new IISLog(e);
        }

        public IISLog(XElement parent)
        {
            Name = ParseRequiredStringAttribute(parent, "name");
            Location = ParseRequiredStringAttribute(parent, "location");
            ICodepage = ParseIntAttribute(parent, "iCodepage", -2);
            Recurse = ParseIntAttribute(parent, "recurse", 0);
            MinDateMod = ParseDateAttribute(parent, "minDateMod");
            Locale = ParseStringAttribute(parent, "locale", "DEF");
            ICheckpoint = ParseStringAttribute(parent, "iCheckpoint");
            parseFields(parent);
        }

        private void parseFields(XElement parent)
        {
            Dictionary<string, Type> allPossibleFields = new Dictionary<string, Type>()
            {
                { "LogFilename", typeof(string) },
                { "LogRow", typeof(int) },
                { "UserIP", typeof(string) },
                { "UserName", typeof(string) },
                { "Date", typeof(DateTime) },
                { "Time", typeof(DateTime) },
                { "ServiceInstance", typeof(string) },
                { "HostName", typeof(string) },
                { "ServerIP", typeof(string) },
                { "TimeTaken", typeof(int) },
                { "BytesSent", typeof(int) },
                { "BytesReceived", typeof(int) },
                { "StatusCode", typeof(int) },
                { "Win32StatusCode", typeof(int) },
                { "RequestType", typeof(string) },
                { "Target", typeof(string) },
                { "Parameters", typeof(string) }
            };

            Fields = ParseFields(parent, allPossibleFields);
        }

    }
}

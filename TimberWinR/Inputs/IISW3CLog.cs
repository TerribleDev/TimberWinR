using System;
using System.Collections.Generic;
using System.Text;
using System.Xml.Linq;

namespace TimberWinR.Inputs
{
    public class IISW3CLog : InputBase
    {
        public string Name { get; private set; }
        public string Location { get; private set; }
        public int ICodepage { get; private set; }
        public int Recurse { get; private set; }
        public string MinDateMod { get; private set; }
        public bool DQuotes { get; private set; }
        public bool DirTime { get; private set; }
        public bool ConsolidateLogs { get; private set; }
        public string ICheckpoint { get; private set; }
        public List<FieldDefinition> Fields { get; private set; }

        public static void Parse(List<IISW3CLog> iisw3clogs, XElement iisw3clogElement)
        {
            iisw3clogs.Add(parseIISW3CLog(iisw3clogElement));
        }

        static IISW3CLog parseIISW3CLog(XElement e)
        {
            return new IISW3CLog(e);
        }

        public IISW3CLog(XElement parent)
        {
            Name = ParseRequiredStringAttribute(parent, "name");
            Location = ParseRequiredStringAttribute(parent, "location");
            ICodepage = ParseIntAttribute(parent, "iCodepage", -2);
            Recurse = ParseIntAttribute(parent, "recurse", 0);
            DQuotes = ParseBoolAttribute(parent, "dQuotes", false);
            DirTime = ParseBoolAttribute(parent, "dirTime", false);
            ConsolidateLogs = ParseBoolAttribute(parent, "consolidateLogs", false);
            ICheckpoint = ParseStringAttribute(parent, "iCheckpoint");
            ParseFields(parent);
        }

        private void ParseFields(XElement parent)
        {
            Dictionary<string, Type> allPossibleFields = new Dictionary<string, Type>()
            {
                { "LogFilename", typeof(string) },
                { "LogRow", typeof(int) },
                { "date", typeof(DateTime) },
                { "time", typeof(DateTime) },
                { "c-ip", typeof(string) },
                { "cs-username", typeof(string) },
                { "s-sitename", typeof(string) },
                { "s-computername", typeof(int) },
                { "s-ip", typeof(string) },
                { "s-port", typeof(int) },
                { "cs-method", typeof(string) },
                { "cs-uri-stem", typeof(string) },
                { "cs-uri-query", typeof(string) },
                { "sc-status", typeof(int) },
                { "sc-substatus", typeof(int) },
                { "sc-win32-status", typeof(int) },
                { "sc-bytes", typeof(int) },
                { "cs-bytes", typeof(int) },
                { "time-taken", typeof(int) },
                { "cs-version", typeof(string) },
                { "cs-host", typeof(string) },
                { "cs(User-Agent)", typeof(string) },
                { "cs(Cookie)", typeof(string) },
                { "cs(Referer)", typeof(string) },
                { "s-event", typeof(string) },
                { "s-process-type", typeof(string) },
                { "s-user-time", typeof(double) },
                { "s-kernel-time", typeof(double) },
                { "s-page-faults", typeof(int) },
                { "s-total-procs", typeof(int) },
                { "s-active-procs", typeof(int) },
                { "s-stopped-procs", typeof(int) }
            };

            Fields = base.parseFields(parent, allPossibleFields);            
        }

    }
}

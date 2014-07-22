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
        public List<FieldDefinition> Fields { get; private set; }

        // Parameters
        public int ICodepage { get; private set; }
        public int Recurse { get; private set; }
        public string MinDateMod { get; private set; }
        public bool DQuotes { get; private set; }
        public bool DirTime { get; private set; }
        public bool ConsolidateLogs { get; private set; }
        public string ICheckpoint { get; private set; }

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
            ParseName(parent);
            ParseLocation(parent);

            // Default values for parameters.
            ICodepage = -2;
            Recurse = 0;
            DQuotes = false;
            DirTime = false;
            ConsolidateLogs = false;

            ParseICodepage(parent);
            ParseRecurse(parent);
            ParseDQuotes(parent);
            ParseDirTime(parent);
            ParseConsolidateLogs(parent);
            ParseICheckpoint(parent);

            ParseFields(parent);
        }

        private void ParseName(XElement parent)
        {
            string attributeName = "name";

            try
            {
                XAttribute a = parent.Attribute(attributeName);

                Name = a.Value;
            }
            catch
            {
                throw new TimberWinR.ConfigurationErrors.MissingRequiredAttributeException(parent, attributeName);
            }
        }

        private void ParseLocation(XElement parent)
        {
            string attributeName = "location";

            try
            {
                XAttribute a = parent.Attribute(attributeName);

                Location = a.Value;
            }
            catch
            {
                throw new TimberWinR.ConfigurationErrors.MissingRequiredAttributeException(parent, attributeName);
            }
        }

        private void ParseICodepage(XElement parent)
        {
            string attributeName = "iCodepage";
            int valInt;

            try
            {
                XAttribute a = parent.Attribute(attributeName);

                if (int.TryParse(a.Value, out valInt))
                {
                    ICodepage = valInt;
                }
                else
                {
                    throw new TimberWinR.ConfigurationErrors.InvalidAttributeIntegerValueException(a);
                }
            }
            catch
            {
            }
        }

        private void ParseRecurse(XElement parent)
        {
            string attributeName = "recurse";
            int valInt;

            try
            {
                XAttribute a = parent.Attribute(attributeName);

                if (int.TryParse(a.Value, out valInt))
                {
                    Recurse = valInt;
                }
                else
                {
                    throw new TimberWinR.ConfigurationErrors.InvalidAttributeIntegerValueException(a);
                }
            }
            catch
            {
            }
        }

        private void ParseDQuotes(XElement parent)
        {
            string attributeName = "dQuotes";
            string value;

            try
            {
                XAttribute a = parent.Attribute(attributeName);

                value = a.Value;

                if (value == "ON" || value == "true")
                {
                    DQuotes = true;
                }
                else if (value == "OFF" || value == "false")
                {
                    DQuotes = false;
                }
                else
                {
                    throw new TimberWinR.ConfigurationErrors.InvalidAttributeValueException(parent.Attribute(attributeName));
                }
            }
            catch { }
        }

        private void ParseDirTime(XElement parent)
        {
            string attributeName = "dirTime";
            string value;

            try
            {
                XAttribute a = parent.Attribute(attributeName);

                value = a.Value;

                if (value == "ON" || value == "true")
                {
                    DirTime = true;
                }
                else if (value == "OFF" || value == "false")
                {
                    DirTime = false;
                }
                else
                {
                    throw new TimberWinR.ConfigurationErrors.InvalidAttributeValueException(parent.Attribute(attributeName));
                }
            }
            catch { }
        }

        private void ParseConsolidateLogs(XElement parent)
        {
            string attributeName = "consolidateLogs";
            string value;

            try
            {
                XAttribute a = parent.Attribute(attributeName);

                value = a.Value;

                if (value == "ON" || value == "true")
                {
                    ConsolidateLogs = true;
                }
                else if (value == "OFF" || value == "false")
                {
                    ConsolidateLogs = false;
                }
                else
                {
                    throw new TimberWinR.ConfigurationErrors.InvalidAttributeValueException(parent.Attribute(attributeName));
                }
            }
            catch { }
        }

        private void ParseICheckpoint(XElement parent)
        {
            string attributeName = "iCheckpoint";

            try
            {
                XAttribute a = parent.Attribute(attributeName);

                ICheckpoint = a.Value;
            }
            catch { }
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

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("IISW3CLog\n");
            sb.Append(String.Format("Name: {0}\n", Name));
            sb.Append(String.Format("Location: {0}\n", Location));
            sb.Append("Fields:\n");
            foreach (FieldDefinition f in Fields)
            {
                sb.Append(String.Format("\t{0}\n", f.Name));
            }
            sb.Append("Parameters:\n");
            sb.Append(String.Format("\tiCodepage: {0}\n", ICodepage));
            sb.Append(String.Format("\trecurse: {0}\n", Recurse));
            sb.Append(String.Format("\tminDateMod: {0}\n", MinDateMod));
            sb.Append(String.Format("\tdQuotes: {0}\n", DQuotes));
            sb.Append(String.Format("\tdirTime: {0}\n", DirTime));
            sb.Append(String.Format("\tconsolidateLogs: {0}\n", ConsolidateLogs));
            sb.Append(String.Format("\tiCheckpoint: {0}\n", ICheckpoint));

            return sb.ToString();
        }
    }
}

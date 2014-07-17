using System;
using System.Collections.Generic;
using System.Data.Odbc;
using System.Linq;
using System.Text;
using System.Xml;
using System.Xml.Linq;
using System.IO;
using System.Globalization;
using TimberWinR.Inputs;
namespace TimberWinR
{
    public class Configuration
    {
        private class InvalidAttributeValueException : Exception
        {
            public InvalidAttributeValueException(XAttribute a, string badValue)
                : base(
                    string.Format("{0}:{1} Invalid Attribute <{2} {3}=\"{4}\">", a.Document.BaseUri,
                        ((IXmlLineInfo)a).LineNumber, a.Parent.Name, a.Name, badValue))
            {
            }
        }

        private static List<WindowsEvents> _events = new List<WindowsEvents>();
        public IEnumerable<WindowsEvents> Events { get { return _events; } }

        private static List<TextLogs> _logs = new List<TextLogs>();
        public IEnumerable<TextLogs> Logs { get { return _logs; } }

        private static List<IISLogs> _iislogs = new List<IISLogs>();
        public IEnumerable<IISLogs> IIS { get { return _iislogs; } }

        public Configuration(string xmlConfFile)
        {
            parseXMLConf(xmlConfFile);
        }

        static List<FieldDefinition> parseFields_Events(IEnumerable<XElement> xml_fields)
        {
            List<FieldDefinition> fields = new List<FieldDefinition>();

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

            foreach (XElement f in xml_fields)
            {
                string name = f.Attribute("name").Value;
                if (allPossibleFields.ContainsKey(name))
                {
                    fields.Add(new FieldDefinition(name, allPossibleFields[name]));
                }
                else
                {
                    Console.WriteLine(String.Format("ERROR. WindowsEvents encountered unknown field name: {0}", name));
                }
            }

            if (fields.Count == 0)
            {
                foreach (KeyValuePair<string, Type> entry in allPossibleFields)
                {
                    fields.Add(new FieldDefinition(entry.Key, entry.Value));
                }
            }

            return fields;
        }

        static List<FieldDefinition> parseFields_Logs(IEnumerable<XElement> xml_fields)
        {
            List<FieldDefinition> fields = new List<FieldDefinition>();

            Dictionary<string, Type> allPossibleFields = new Dictionary<string, Type>()
            {
                { "LogFilename", typeof(string) },
                { "Index", typeof(int) },
                { "Text", typeof(string) }
            };

            foreach (XElement f in xml_fields)
            {
                string name = f.Attribute("name").Value;
                if (allPossibleFields.ContainsKey(name))
                {
                    fields.Add(new FieldDefinition(name, allPossibleFields[name]));
                }
                else
                {
                    Console.WriteLine(String.Format("ERROR. Logs encountered unknown field name: {0}", name));
                }
            }

            if (fields.Count == 0)
            {
                foreach (KeyValuePair<string, Type> entry in allPossibleFields)
                {
                    fields.Add(new FieldDefinition(entry.Key, entry.Value));
                }
            }

            return fields;
        }

        static List<FieldDefinition> parseFields_IIS(IEnumerable<XElement> xml_fields)
        {
            List<FieldDefinition> fields = new List<FieldDefinition>();

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

            foreach (XElement f in xml_fields)
            {
                string name = f.Attribute("name").Value;
                if (allPossibleFields.ContainsKey(name))
                {
                    fields.Add(new FieldDefinition(name, allPossibleFields[name]));
                }
                else
                {
                    Console.WriteLine(String.Format("ERROR. IIS Logs encountered unknown field name: {0}", name));
                }
            }

            if (fields.Count == 0)
            {
                foreach (KeyValuePair<string, Type> entry in allPossibleFields)
                {
                    fields.Add(new FieldDefinition(entry.Key, entry.Value));
                }
            }

            return fields;
        }

        static Params_WindowsEvents parseParams_Events(IEnumerable<XAttribute> attributes)
        {
            Params_WindowsEvents.Builder p = new Params_WindowsEvents.Builder();

            foreach (XAttribute a in attributes)
            {
                string val = a.Value;
                IXmlLineInfo li = ((IXmlLineInfo)a);

                switch (a.Name.ToString())
                {
                    case "source":
                        break;
                    case "fullText":
                        if (val == "ON" || val == "true")
                        {
                            p.WithFullText(true);
                        }
                        else if (val == "OFF" || val == "false")
                        {
                            p.WithFullText(false);
                        }
                        else
                        {
                            throw new InvalidAttributeValueException(a, val);
                        }
                        break;
                    case "resolveSIDS":
                        if (val == "ON" || val == "true")
                        {
                            p.WithResolveSIDS(true);
                        }
                        else if (val == "OFF" || val == "false")
                        {
                            p.WithResolveSIDS(false);
                        }
                        else
                        {
                            throw new InvalidAttributeValueException(a, val);
                        }
                        break;
                    case "formatMsg":
                        if (val == "ON" || val == "true")
                        {
                            p.WithFormatMsg(true);
                        }
                        else if (val == "OFF" || val == "false")
                        {
                            p.WithFormatMsg(false);
                        }
                        else
                        {
                            throw new InvalidAttributeValueException(a, val);
                        }
                        break;
                    case "msgErrorMode":
                        if (val == "NULL" || val == "ERROR" || val == "MSG")
                        {
                            p.WithMsgErrorMode(val);
                        }
                        else
                        {
                            throw new InvalidAttributeValueException(a, val);
                        }
                        break;
                    case "fullEventCode":
                        if (val == "ON" || val == "true")
                        {
                            p.WithFullEventCode(true);
                        }
                        else if (val == "OFF" || val == "false")
                        {
                            p.WithFullEventCode(false);
                        }
                        else
                        {
                            throw new InvalidAttributeValueException(a, val);
                        }
                        break;
                    case "direction":
                        if (val == "FW" || val == "BW")
                        {
                            p.WithDirection(val);
                        }
                        else
                        {
                            throw new InvalidAttributeValueException(a, val);
                        }
                        break;
                    case "stringsSep":
                        p.WithStringsSep(val);
                        break;
                    case "iCheckpoint":
                        p.WithICheckpoint(val);
                        break;
                    case "binaryFormat":
                        if (val == "ASC" || val == "PRINT" || val == "HEX")
                        {
                            p.WithBinaryFormat(val);
                        }
                        else
                        {
                            throw new InvalidAttributeValueException(a, val);
                        }
                        break;
                    default:
                        throw new Exception(String.Format("ERROR. WindowsEvents encountered unknown attribute: {0}.", a.Name.ToString()));
                        break;
                }
            }

            return p.Build();
        }

        static Params_TextLogs parseParams_Logs(IEnumerable<XAttribute> attributes)
        {
            Params_TextLogs.Builder p = new Params_TextLogs.Builder();

            foreach (XAttribute a in attributes)
            {
                string val = a.Value;
                int valInt;

                switch (a.Name.ToString())
                {
                    case "name":
                        break;
                    case "location":
                        break;
                    case "iCodepage":
                        if (int.TryParse(val, out valInt))
                        {
                            p.WithICodepage(valInt);
                        }
                        else
                        {
                            Console.WriteLine("ERROR. Integer value not given for Logs:iCodepage.");
                        }
                        break;
                    case "recurse":
                        if (int.TryParse(val, out valInt))
                        {
                            p.WithRecurse(valInt);
                        }
                        else
                        {
                            Console.WriteLine("ERROR. Integer value not given for Logs:recurse.");
                        }
                        break;
                    case "splitLongLines":
                        if (val == "ON" || val == "true")
                        {
                            p.WithSplitLongLines(true);
                        }
                        else if (val == "OFF" || val == "false")
                        {
                            p.WithSplitLongLines(false);
                        }
                        else
                        {
                            throw new InvalidAttributeValueException(a, val);
                        }
                        break;
                    case "iCheckpoint":
                        p.WithICheckpoint(val);
                        break;
                    default:
                        Console.WriteLine(String.Format("ERROR. Logs encountered unknown attribute: {0}.", a.Name.ToString()));
                        break;
                }
            }

            return p.Build();
        }

        static Params_IISLogs parseParams_IIS(IEnumerable<XAttribute> attributes)
        {
            Params_IISLogs.Builder p = new Params_IISLogs.Builder();

            foreach (XAttribute a in attributes)
            {
                string val = a.Value;
                int valInt;

                switch (a.Name.ToString())
                {
                    case "name":
                        break;
                    case "location":
                        break;
                    case "iCodepage":
                        if (int.TryParse(val, out valInt))
                        {
                            p.WithICodepage(valInt);
                        }
                        else
                        {
                            Console.WriteLine("ERROR. Integer value not given for Logs:iCodepage.");
                        }
                        break;
                    case "recurse":
                        if (int.TryParse(val, out valInt))
                        {
                            p.WithRecurse(valInt);
                        }
                        else
                        {
                            Console.WriteLine("ERROR. Integer value not given for Logs:recurse.");
                        }
                        break;
                    case "minDateMod":
                        DateTime dt;
                        if (DateTime.TryParseExact(val,
                            "yyyy-MM-dd hh:mm:ss",
                            CultureInfo.InvariantCulture,
                            DateTimeStyles.None,
                            out dt))
                        {
                            p.WithMinDateMod(val);
                        }
                        else
                        {
                            Console.WriteLine("ERROR. Invalid date format given for Logs:minDateMod. Date format must be yyyy-MM-dd hh:mm:ss");
                        }
                        break;
                    case "locale":
                        p.WithLocale(val);
                        break;
                    case "iCheckpoint":
                        p.WithICheckpoint(val);
                        break;
                    default:
                        Console.WriteLine(String.Format("ERROR. IIS Logs encountered unknown attribute: {0}.", a.Name.ToString()));
                        break;
                }
            }

            return p.Build();
        }

        static void parseXMLConf(string xmlConfFile)
        {
            XDocument config = XDocument.Load(xmlConfFile, LoadOptions.SetLineInfo | LoadOptions.SetBaseUri);

            IEnumerable<XElement> inputs =
                from el in config.Root.Descendants("Inputs")
                select el;

            // WINDOWS EVENTSexc
            IEnumerable<XElement> xml_events =
                from el in inputs.Descendants("WindowsEvents").Descendants("Events")
                select el;

            foreach (XElement e in xml_events)
            {
                string source = e.Attribute("source").Value;

                IEnumerable<XElement> xml_fields =
                    from el in e.Descendants("Fields").Descendants("Field")
                    select el;
                List<FieldDefinition> fields = parseFields_Events(xml_fields);

                Params_WindowsEvents args = parseParams_Events(e.Attributes());

                WindowsEvents evt = new WindowsEvents(source, fields, args);
                _events.Add(evt);
            }



            // TEXT LOGS
            IEnumerable<XElement> xml_logs =
                from el in inputs.Descendants("Logs").Descendants("Log")
                select el;

            foreach (XElement e in xml_logs)
            {
                string name = e.Attribute("name").Value;
                string location = e.Attribute("location").Value;

                IEnumerable<XElement> xml_fields =
                    from el in e.Descendants("Fields").Descendants("Field")
                    select el;
                List<FieldDefinition> fields = parseFields_Logs(xml_fields);

                Params_TextLogs args = parseParams_Logs(e.Attributes());

                TextLogs log = new TextLogs(name, location, fields, args);
                _logs.Add(log);
            }



            // IIS LOGS
            IEnumerable<XElement> xml_iis =
                from el in inputs.Descendants("IISLogs").Descendants("IISLog")
                select el;
            foreach (XElement e in xml_iis)
            {
                string name = e.Attribute("name").Value;
                string location = e.Attribute("location").Value;

                IEnumerable<XElement> xml_fields =
                    from el in e.Descendants("Fields").Descendants("Field")
                    select el;
                List<FieldDefinition> fields = parseFields_IIS(xml_fields);

                Params_IISLogs args = parseParams_IIS(e.Attributes());


                IISLogs iis = new IISLogs(name, location, fields, args);
                _iislogs.Add(iis);
            }


            Console.WriteLine("end");
        }

        public class WindowsEvents
        {
            public string Source { get; private set; }
            public List<FieldDefinition> Fields { get; private set; }

            // Parameters
            public bool FullText { get; private set; }
            public bool ResolveSIDS { get; private set; }
            public bool FormatMsg { get; private set; }
            public string MsgErrorMode { get; private set; }
            public bool FullEventCode { get; private set; }
            public string Direction { get; private set; }
            public string StringsSep { get; private set; }
            public string ICheckpoint { get; private set; }
            public string BinaryFormat { get; private set; }

            public WindowsEvents(string source, List<FieldDefinition> fields, Params_WindowsEvents args)
            {
                Source = source;
                Fields = fields;

                FullText = args.FullText;
                ResolveSIDS = args.ResolveSIDS;
                FormatMsg = args.FormatMsg;
                MsgErrorMode = args.MsgErrorMode;
                Direction = args.Direction;
                StringsSep = args.StringsSep;
                ICheckpoint = args.ICheckpoint;
                BinaryFormat = args.BinaryFormat;
            }

            public override string ToString()
            {
                StringBuilder sb = new StringBuilder();
                sb.Append("WindowsEvent\n");
                sb.Append(String.Format("Source: {0}\n", Source));
                sb.Append("Fields:\n");
                foreach (FieldDefinition f in Fields)
                {
                    sb.Append(String.Format("\t{0}\n", f.Name));
                }
                sb.Append("Parameters:\n");
                sb.Append(String.Format("\tfullText: {0}\n", FullText));
                sb.Append(String.Format("\tresolveSIDS: {0}\n", ResolveSIDS));
                sb.Append(String.Format("\tformatMsg: {0}\n", FormatMsg));
                sb.Append(String.Format("\tmsgErrorMode: {0}\n", MsgErrorMode));
                sb.Append(String.Format("\tfullEventCode: {0}\n", FullEventCode));
                sb.Append(String.Format("\tdirection: {0}\n", Direction));
                sb.Append(String.Format("\tstringsSep: {0}\n", StringsSep));
                sb.Append(String.Format("\tiCheckpoint: {0}\n", ICheckpoint));
                sb.Append(String.Format("\tbinaryFormat: {0}\n", BinaryFormat));

                return sb.ToString();
            }
        }

        public class TextLogs
        {
            public string Name { get; private set; }
            public string Location { get; private set; }
            public List<FieldDefinition> Fields { get; private set; }

            // Parameters
            public int ICodepage { get; private set; }
            public int Recurse { get; private set; }
            public bool SplitLongLines { get; private set; }
            public string ICheckpoint { get; private set; }

            public TextLogs(string name, string location, List<FieldDefinition> fields, Params_TextLogs args)
            {
                Name = name;
                Location = location;
                Fields = fields;

                ICodepage = args.ICodepage;
                Recurse = args.Recurse;
                SplitLongLines = args.SplitLongLines;
                ICheckpoint = args.ICheckpoint;
            }

            public override string ToString()
            {
                StringBuilder sb = new StringBuilder();
                sb.Append("TextLog\n");
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
                sb.Append(String.Format("\tsplitLongLines: {0}\n", SplitLongLines));
                sb.Append(String.Format("\tiCheckpoint: {0}\n", ICheckpoint));

                return sb.ToString();
            }
        }

        public class IISLogs
        {
            public string Name { get; private set; }
            public string Location { get; private set; }
            public List<FieldDefinition> Fields { get; private set; }

            // Parameters
            public int ICodepage { get; private set; }
            public int Recurse { get; private set; }
            public string MinDateMod { get; private set; }
            public string Locale { get; private set; }
            public string ICheckpoint { get; private set; }

            public IISLogs(string name, string location, List<FieldDefinition> fields, Params_IISLogs args)
            {
                Name = name;
                Location = location;
                Fields = fields;

                ICodepage = args.ICodepage;
                Recurse = args.Recurse;
                MinDateMod = args.MinDateMod;
                Locale = args.Locale;
                ICheckpoint = args.ICheckpoint;
            }

            public override string ToString()
            {
                StringBuilder sb = new StringBuilder();
                sb.Append("TextLog\n");
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
                sb.Append(String.Format("\tlocale: {0}\n", Locale));
                sb.Append(String.Format("\tiCheckpoint: {0}\n", ICheckpoint));

                return sb.ToString();
            }
        }

        public class Params_WindowsEvents
        {
            public bool FullText { get; private set; }
            public bool ResolveSIDS { get; private set; }
            public bool FormatMsg { get; private set; }
            public string MsgErrorMode { get; private set; }
            public bool FullEventCode { get; private set; }
            public string Direction { get; private set; }
            public string StringsSep { get; private set; }
            public string ICheckpoint { get; private set; }
            public string BinaryFormat { get; private set; }

            public class Builder
            {
                // Default values for parameters.
                private bool fullText = true;
                private bool resolveSIDS = true;
                private bool formatMsg = true;
                private string msgErrorMode = "MSG";
                private bool fullEventCode = false;
                private string direction = "FW";
                private string stringsSep = "|";
                private string iCheckpoint;
                private string binaryFormat = "PRINT";

                public Builder WithFullText(bool value)
                {
                    fullText = value;
                    return this;
                }

                public Builder WithResolveSIDS(bool value)
                {
                    resolveSIDS = value;
                    return this;
                }

                public Builder WithFormatMsg(bool value)
                {
                    formatMsg = value;
                    return this;
                }

                public Builder WithMsgErrorMode(string value)
                {
                    msgErrorMode = value;
                    return this;
                }

                public Builder WithFullEventCode(bool value)
                {
                    fullEventCode = value;
                    return this;
                }

                public Builder WithDirection(string value)
                {
                    direction = value;
                    return this;
                }

                public Builder WithStringsSep(string value)
                {
                    stringsSep = value;
                    return this;
                }

                public Builder WithICheckpoint(string value)
                {
                    iCheckpoint = value;
                    return this;
                }

                public Builder WithBinaryFormat(string value)
                {
                    binaryFormat = value;
                    return this;
                }

                public Params_WindowsEvents Build()
                {
                    return new Params_WindowsEvents()
                    {
                        FullText = fullText,
                        ResolveSIDS = resolveSIDS,
                        FormatMsg = formatMsg,
                        MsgErrorMode = msgErrorMode,
                        FullEventCode = fullEventCode,
                        Direction = direction,
                        StringsSep = stringsSep,
                        ICheckpoint = iCheckpoint,
                        BinaryFormat = binaryFormat
                    };
                }
            }
        }

        public class Params_TextLogs
        {
            public int ICodepage { get; private set; }
            public int Recurse { get; private set; }
            public bool SplitLongLines { get; private set; }
            public string ICheckpoint { get; private set; }

            public class Builder
            {
                // Default values for parameters.
                private int iCodepage = 0;
                private int recurse = 0;
                private bool splitLongLines = false;
                private string iCheckpoint;

                public Builder WithICodepage(int value)
                {
                    iCodepage = value;
                    return this;
                }

                public Builder WithRecurse(int value)
                {
                    recurse = value;
                    return this;
                }

                public Builder WithSplitLongLines(bool value)
                {
                    splitLongLines = value;
                    return this;
                }

                public Builder WithICheckpoint(string value)
                {
                    iCheckpoint = value;
                    return this;
                }

                public Params_TextLogs Build()
                {
                    return new Params_TextLogs()
                    {
                        ICodepage = iCodepage,
                        Recurse = recurse,
                        SplitLongLines = splitLongLines,
                        ICheckpoint = iCheckpoint
                    };
                }
            }


        }

        public class Params_IISLogs
        {
            public int ICodepage { get; private set; }
            public int Recurse { get; private set; }
            public string MinDateMod { get; private set; }
            public string Locale { get; private set; }
            public string ICheckpoint { get; private set; }

            public class Builder
            {
                // Default values for parameters.
                private int iCodepage = -2;
                private int recurse = 0;
                private string minDateMod;
                private string locale = "DEF";
                private string iCheckpoint;

                public Builder WithICodepage(int value)
                {
                    iCodepage = value;
                    return this;
                }

                public Builder WithRecurse(int value)
                {
                    recurse = value;
                    return this;
                }

                public Builder WithMinDateMod(string value)
                {
                    minDateMod = value;
                    return this;
                }

                public Builder WithLocale(string value)
                {
                    locale = value;
                    return this;
                }

                public Builder WithICheckpoint(string value)
                {
                    iCheckpoint = value;
                    return this;
                }

                public Params_IISLogs Build()
                {
                    return new Params_IISLogs()
                    {
                        ICodepage = iCodepage,
                        Recurse = recurse,
                        MinDateMod = minDateMod,
                        Locale = locale,
                        ICheckpoint = iCheckpoint
                    };
                }
            }
        }

    }
}

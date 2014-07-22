using System;
using System.Collections.Generic;
using System.Text;
using System.Xml.Linq;

namespace TimberWinR.Inputs
{
    public class WindowsEvent : InputBase
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

            ParseSource(parent);

            // Default values for parameters.
            FullText = true;
            ResolveSIDS = true;
            FormatMsg = true;
            MsgErrorMode = "MSG";
            FullEventCode = false;
            Direction = "FW";
            StringsSep = "|";
            BinaryFormat = "PRINT";

            ParseFullText(parent);
            ParseResolveSIDS(parent);
            ParseFormatMsg(parent);
            ParseMsgErrorMode(parent);
            ParseFullEventCode(parent);
            ParseDirection(parent);
            ParseStringsSep(parent);
            ParseBinaryFormat(parent);

            ParseFields(parent);
        }

        private void ParseSource(XElement parent)
        {
            string attributeName = "source";

            try
            {
                XAttribute a = parent.Attribute(attributeName);

                Source = a.Value;
            }
            catch
            {
                throw new TimberWinR.ConfigurationErrors.MissingRequiredAttributeException(parent, attributeName);
            }
        }

        private void ParseFullText(XElement parent)
        {
            string attributeName = "fullText";
            string value;

            try
            {
                XAttribute a = parent.Attribute(attributeName);

                value = a.Value;

                if (value == "ON" || value == "true")
                {
                    FullText = true;
                }
                else if (value == "OFF" || value == "false")
                {
                    FullText = false;
                }
                else
                {
                    throw new TimberWinR.ConfigurationErrors.InvalidAttributeValueException(parent.Attribute(attributeName));
                }
            }
            catch { }
        }

        private void ParseResolveSIDS(XElement parent)
        {
            string attributeName = "resolveSIDS";
            string value;

            try
            {
                XAttribute a = parent.Attribute(attributeName);

                value = a.Value;

                if (value == "ON" || value == "true")
                {
                    ResolveSIDS = true;
                }
                else if (value == "OFF" || value == "false")
                {
                    ResolveSIDS = false;
                }
                else
                {
                    throw new TimberWinR.ConfigurationErrors.InvalidAttributeValueException(parent.Attribute(attributeName));
                }
            }
            catch { }
        }

        private void ParseFormatMsg(XElement parent)
        {
            string attributeName = "formatMsg";
            string value;

            try
            {
                XAttribute a = parent.Attribute(attributeName);

                value = a.Value;

                if (value == "ON" || value == "true")
                {
                    FormatMsg = true;
                }
                else if (value == "OFF" || value == "false")
                {
                    FormatMsg = false;
                }
                else
                {
                    throw new TimberWinR.ConfigurationErrors.InvalidAttributeValueException(parent.Attribute(attributeName));
                }
            }
            catch { }
        }

        private void ParseMsgErrorMode(XElement parent)
        {
            string attributeName = "msgErrorMode";
            string value;

            try
            {
                XAttribute a = parent.Attribute(attributeName);

                value = a.Value;

                if (value == "NULL" || value == "ERROR" || value == "MSG")
                {
                    MsgErrorMode = value;
                }
                else
                {
                    throw new TimberWinR.ConfigurationErrors.InvalidAttributeValueException(parent.Attribute(attributeName));
                }
            }
            catch { }
        }

        private void ParseFullEventCode(XElement parent)
        {
            string attributeName = "fullEventCode";
            string value;

            try
            {
                XAttribute a = parent.Attribute(attributeName);

                value = a.Value;

                if (value == "ON" || value == "true")
                {
                    FullEventCode = true;
                }
                else if (value == "OFF" || value == "false")
                {
                    FullEventCode = false;
                }
                else
                {
                    throw new TimberWinR.ConfigurationErrors.InvalidAttributeValueException(parent.Attribute(attributeName));
                }
            }
            catch { }
        }

        private void ParseDirection(XElement parent)
        {
            string attributeName = "direction";
            string value;

            try
            {
                XAttribute a = parent.Attribute(attributeName);

                value = a.Value;

                if (value == "FW" || value == "BW")
                {
                    Direction = value;
                }
                else
                {
                    throw new TimberWinR.ConfigurationErrors.InvalidAttributeValueException(parent.Attribute(attributeName));
                }
            }
            catch { }
        }

        private void ParseStringsSep(XElement parent)
        {
            string attributeName = "stringsSep";

            try
            {
                XAttribute a = parent.Attribute(attributeName);

                StringsSep = a.Value;
            }
            catch { }
        }

        private void ParseBinaryFormat(XElement parent)
        {
            string attributeName = "binaryFormat";
            string value;

            try
            {
                XAttribute a = parent.Attribute(attributeName);

                value = a.Value;

                if (value == "ASC" || value == "PRINT" || value == "HEX")
                {
                    BinaryFormat = value;
                }
                else
                {
                    throw new TimberWinR.ConfigurationErrors.InvalidAttributeValueException(parent.Attribute(attributeName));
                }
            }
            catch { }
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
}
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Xml.Linq;

namespace TimberWinR.Inputs
{
    public class IISLog : InputBase
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
            ParseName(parent);
            ParseLocation(parent);

            // Default values for parameters.
            ICodepage = -2;
            Recurse = 0;
            Locale = "DEF";

            ParseICodepage(parent);
            ParseRecurse(parent);
            ParseMinDateMod(parent);
            ParseLocale(parent);
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

        private void ParseMinDateMod(XElement parent)
        {
            string attributeName = "minDateMod";

            try
            {
                XAttribute a = parent.Attribute(attributeName);

                DateTime dt;
                if (DateTime.TryParseExact(a.Value,
                    "yyyy-MM-dd hh:mm:ss",
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.None,
                    out dt))
                {
                    MinDateMod = a.Value;
                }
                else
                {
                    throw new TimberWinR.ConfigurationErrors.InvalidAttributeDateValueException(a);
                }

                Locale = a.Value;
            }
            catch { } 
        }

        private void ParseLocale(XElement parent)
        {
            string attributeName = "locale";

            try
            {
                XAttribute a = parent.Attribute(attributeName);

                Locale = a.Value;
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

            Fields = base.parseFields(parent, allPossibleFields);
        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("IISLog\n");
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
}

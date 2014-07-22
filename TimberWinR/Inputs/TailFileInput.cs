using System;
using System.Collections.Generic;
using System.Text;
using System.Xml.Linq;

namespace TimberWinR.Inputs
{
    public class TailFileInput : InputBase
    {
        public string Name { get; private set; }
        public string Location { get; private set; }
        public List<FieldDefinition> Fields { get; private set; }

        // Parameters
        public int ICodepage { get; private set; }
        public int Recurse { get; private set; }
        public bool SplitLongLines { get; private set; }
        public string ICheckpoint { get; private set; }

        public static void Parse(List<TailFileInput> logs, XElement logElement)
        {
            logs.Add(parseLog(logElement));
        }

        static TailFileInput parseLog(XElement e)
        {
            return new TailFileInput(e);
        }

        public TailFileInput(XElement parent)
        {
            ParseName(parent);
            ParseLocation(parent);

            // Default values for parameters.
            ICodepage = 0;
            Recurse = 0;
            SplitLongLines = false;

            ParseICodepage(parent);
            ParseRecurse(parent);
            ParseSplitLongLines(parent);
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

        private void ParseSplitLongLines(XElement parent)
        {
            string attributeName = "splitLongLines";
            string value;

            try
            {
                XAttribute a = parent.Attribute(attributeName);

                value = a.Value;

                if (value == "ON" || value == "true")
                {
                    SplitLongLines = true;
                }
                else if (value == "OFF" || value == "false")
                {
                    SplitLongLines = false;
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
                { "Index", typeof(int) },
                { "Text", typeof(string) }
            };

            Fields = base.parseFields(parent, allPossibleFields);
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
}
using System;
using System.Collections.Generic;
using System.Text;
using System.Xml.Linq;

namespace TimberWinR.Inputs
{
    public class TailFileInput : InputBase
    {
        public const string ParentTagName = "Logs";
        public new const string TagName = "Log";

        public string Name { get; private set; }
        public string Location { get; private set; }
        public int ICodepage { get; private set; }
        public int Recurse { get; private set; }
        public bool SplitLongLines { get; private set; }       
        public List<FieldDefinition> Fields { get; private set; }

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
            Name = ParseRequiredStringAttribute(parent, "name");
            Location = ParseRequiredStringAttribute(parent, "location");           
            ICodepage = ParseIntAttribute(parent, "iCodepage", 0);
            Recurse = ParseIntAttribute(parent, "recurse", 0);
            SplitLongLines = ParseBoolAttribute(parent, "splitLongLines", false);                    
            parseFields(parent);
        }       

        private void parseFields(XElement parent)
        {
            Dictionary<string, Type> allPossibleFields = new Dictionary<string, Type>()
            {
                { "LogFilename", typeof(string) },
                { "Index", typeof(int) },
                { "Text", typeof(string) }
            };

            Fields = ParseFields(parent, allPossibleFields);
        }

    }
}
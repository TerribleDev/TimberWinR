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
            ParseFields(parent);
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
                sb.Append(String.Format("\t{0}\n", f.Name));            
            sb.Append("Parameters:\n");
            sb.Append(String.Format("\tiCodepage: {0}\n", ICodepage));
            sb.Append(String.Format("\trecurse: {0}\n", Recurse));
            sb.Append(String.Format("\tsplitLongLines: {0}\n", SplitLongLines));          

            return sb.ToString();
        }
    }
}
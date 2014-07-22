using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using Newtonsoft.Json.Linq;

namespace TimberWinR.Filters
{
    public class AddField
    {
        public string Target { get; set; }
        public string Value { get; set; }
    }

    public class MutateFilter : FilterBase
    {
        public List<Pair> Renames { get; set; }

        public static void Parse(List<FilterBase> filters, XElement rootElement)
        {
            foreach (var e in rootElement.Elements("Filters").Elements("Mutate"))
                filters.Add(parseMutate(e));
        }        
     
        static MutateFilter parseMutate(XElement e)
        {
            return new MutateFilter(e);
        }

        MutateFilter(XElement parent)
        {
            Renames = new List<Pair>();

            ParseRenames(parent);
        }

        private void ParseRenames(XElement parent)
        {
            foreach (var e in parent.Elements("Rename"))
            {
                Pair p = new Pair(e.Attribute("oldName").Value, e.Attribute("newName").Value);
                Renames.Add(p);
            }
        }

        public override void Apply(JObject json)
        {
                     
        }       
    }
}

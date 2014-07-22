using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace TimberWinR.Filters
{
   

    public class MutateFilter : FilterBase
    {
        public const string TagName = "Mutate";

        public List<Rename> Renames { get; private set; }

        public static void Parse(List<FilterBase> filters, XElement mutateElement)
        {
            filters.Add(parseMutate(mutateElement));
        }        
     
        static MutateFilter parseMutate(XElement e)
        {
            return new MutateFilter(e);
        }

        MutateFilter(XElement parent)
        {
            Renames = new List<Rename>();
            ParseRenames(parent);
        }

        private void ParseRenames(XElement parent)
        {
            foreach (var e in parent.Elements("Rename"))
            {
                Rename r = new Rename(e.Attribute("oldName").Value, e.Attribute("newName").Value);
                Renames.Add(r);
            }
        }

        public override void Apply(JObject json)
        {
            foreach (var r in Renames)
                r.Apply(json);
        }

        public class Rename
        {
            public string OldName { get; set; }
            public string NewName { get; set; }

            public Rename(string oldName, string newName)
            {
                OldName = oldName;
                NewName = newName;
            }
            public void Apply(JObject json)
            {
                json = RenameProperty(json, name => name == OldName ? NewName : name) as JObject;
            }

            private static JToken RenameProperty(JToken json, Dictionary<string, string> map)
            {
                return RenameProperty(json, name => map.ContainsKey(name) ? map[name] : name);
            }

            private static JToken RenameProperty(JToken json, Func<string, string> map)
            {
                JProperty prop = json as JProperty;
                if (prop != null)
                {
                    return new JProperty(map(prop.Name), RenameProperty(prop.Value, map));
                }

                JArray arr = json as JArray;
                if (arr != null)
                {
                    var cont = arr.Select(el => RenameProperty(el, map));
                    return new JArray(cont);
                }

                JObject o = json as JObject;
                if (o != null)
                {
                    var cont = o.Properties().Select(el => RenameProperty(el, map));
                    return new JObject(cont);
                }
                return json;
            }
        }
    }
}

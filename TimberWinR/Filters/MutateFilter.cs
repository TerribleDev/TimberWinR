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
        public new const string TagName = "Mutate";

        public List<MutateOperation> Operations { get; private set; }

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
            Operations = new List<MutateOperation>();

            ParseOperations(parent);
        }

        private void ParseOperations(XElement parent)
        {
            foreach (var e in parent.Elements())
            {
                switch(e.Name.ToString())
                {
                    case Rename.TagName:
                        ParseRename(e);
                        break;
                    case Remove.TagName:
                        ParseRemove(e);
                        break;
                }                  
            }                  
        }      

        public override void Apply(JObject json)
        {
            foreach (var r in Operations)
                r.Apply(json);
        }

        public abstract class MutateOperation
        {
            public abstract void Apply(JObject json);
        }

        private void ParseRemove(XElement e)
        {
            var o = new Remove(e.Attribute("field").Value);
            Operations.Add(o);
        }
        
        class Remove : MutateOperation
        {
            public const string TagName = "Remove";
            public string FieldName { get; set; }

            public Remove(string fieldName)
            {
                FieldName = fieldName;
            }

            public override void Apply(JObject json)
            {
                var fieldName = FieldName;
                if (fieldName.Contains('%'))
                    fieldName = ReplaceTokens(fieldName, json);
                json.Remove(fieldName);
            }

            private string ReplaceTokens(string fieldName, JObject json)
            {
                foreach(var token in json.Children())
                {
                    string replaceString = "%{" + token.Path + "}";
                    fieldName = fieldName.Replace(replaceString, json[token.Path].ToString());
                }
                return fieldName;
            }
        }

        private void ParseRename(XElement e)
        {
            var o = new Rename(e.Attribute("oldName").Value, e.Attribute("newName").Value);
            Operations.Add(o);
        }

        class Rename : MutateOperation
        {
            public const string TagName = "Rename";
            public string OldName { get; set; }
            public string NewName { get; set; }

            public Rename(string oldName, string newName)
            {
                OldName = oldName;
                NewName = newName;
            }
           
            public override void Apply(JObject json)
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

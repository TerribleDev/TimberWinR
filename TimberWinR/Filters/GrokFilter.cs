using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using Microsoft.CSharp;
using Newtonsoft.Json.Linq;
using NLog;
using RapidRegex.Core;

namespace TimberWinR.Parser
{
    public class Fields
    {
        private Dictionary<string, string> fields { get; set; }

        public string this[string i]
        {
            get { return fields[i]; }
            set { fields[i] = value; }
        }

        public Fields(JObject json)
        {
            fields = new Dictionary<string, string>();
            IList<string> keys = json.Properties().Select(p => p.Name).ToList();
            foreach (string key in keys)
                fields[key] = json[key].ToString();
        }
    }

    public partial class Grok : LogstashFilter
    {
        // Returns: true - Filter does not apply or has been applied successfully
        // Returns: false - Drop this object
        public override bool Apply(JObject json)
        {
            if (!string.IsNullOrEmpty(Type))
            {
                JToken json_type = json["type"];
                if (json_type != null && json_type.ToString() != Type)
                    return true; // Filter does not apply.
            }
             
            if (Matches(json))
            {
                if (Condition != null)
                {
                    var expr = EvaluateCondition(json, Condition);
                    if (expr)
                    {
                        if (DropIfMatch)
                            return false; // drop this one
                    }
                    else                   
                        return true;                   
                }

                if (DropIfMatch)
                    return false;

                AddFields(json);
                AddTags(json);               
                RemoveFields(json);
                RemoveTags(json);                                
            }

            return true;
        }
      
        private bool Matches(Newtonsoft.Json.Linq.JObject json)
        {
            string field = Match[0];
            string expr = Match[1];

            JToken token = null;
            if (json.TryGetValue(field, out token))
            {
                string text = token.ToString();
                if (!string.IsNullOrEmpty(text))
                {
                    var resolver = new RegexGrokResolver();
                    var pattern = resolver.ResolveToRegex(expr);
                    var match = Regex.Match(text, pattern);
                    if (match.Success)
                    {
                        var regex = new Regex(pattern);
                        var namedCaptures = regex.MatchNamedCaptures(text);
                        foreach (string fieldName in namedCaptures.Keys)
                        {
                            AddOrModify(json, fieldName, namedCaptures[fieldName]);
                        }
                        return true; // Yes!
                    }
                }
                return true; // Empty field is no match
            }
            return false; // Not specified is failure
        }

        private void AddFields(Newtonsoft.Json.Linq.JObject json)
        {
            if (AddField != null && AddField.Length > 0)
            {
                for (int i = 0; i < AddField.Length; i += 2)
                {
                    string fieldName = ExpandField(AddField[i], json);
                    string fieldValue = ExpandField(AddField[i + 1], json);
                    AddOrModify(json, fieldName, fieldValue);
                }
            }
        }      

        private void RemoveFields(Newtonsoft.Json.Linq.JObject json)
        {
            if (RemoveField != null && RemoveField.Length > 0)
            {
                for (int i = 0; i < RemoveField.Length; i++)
                {
                    string fieldName = ExpandField(RemoveField[i], json);
                    RemoveProperties(json, new string[] { fieldName });
                }
            }
        }

        private void AddTags(Newtonsoft.Json.Linq.JObject json)
        {
            if (AddTag != null && AddTag.Length > 0)
            {
                for (int i = 0; i < AddTag.Length; i++)
                {
                    string value = ExpandField(AddTag[i], json);

                    JToken tags = json["tags"];
                    if (tags == null)
                        json.Add("tags", new JArray(value));
                    else
                    {
                        JArray a = tags as JArray;
                        a.Add(value);
                    }
                }
            }
        }

        private void RemoveTags(Newtonsoft.Json.Linq.JObject json)
        {
            if (RemoveTag != null && RemoveTag.Length > 0)
            {
                JToken tags = json["tags"];
                if (tags != null)
                {
                    List<JToken> children = tags.Children().ToList();                                      
                    for (int i = 0; i < RemoveTag.Length; i++)
                    {
                        string tagName = ExpandField(RemoveTag[i], json);       
                        foreach(JToken token in children)
                        {
                            if (token.ToString() == tagName)
                                token.Remove();
                        }
                    }                   
                }               
            }
        }
    }
}

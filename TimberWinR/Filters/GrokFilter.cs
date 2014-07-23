using Newtonsoft.Json.Linq;
using RapidRegex.Core;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace TimberWinR.Filters
{
    public class GrokFilter : FilterBase
    {
        public new const string TagName = "Grok";

        public string Match { get; private set; }
        public string Field { get; private set; }
        public List<FieldValuePair> AddFields { get; private set; }
        public bool DropIfMatch { get; private set; }
        public List<string> RemoveFields { get; private set; }
        public List<AddTag> AddTags { get; private set; } 

        public static void Parse(List<FilterBase> filters, XElement grokElement)
        {
            filters.Add(parseGrok(grokElement));
        }

        static GrokFilter parseGrok(XElement e)
        {
            return new GrokFilter(e);
        }

        GrokFilter(XElement parent)
        {
            AddTags = new List<AddTag>();
            AddFields = new List<FieldValuePair>();
            RemoveFields = new List<string>();

            ParseMatch(parent);
            ParseAddFields(parent);
            ParseAddTags(parent);
            ParseDropIfMatch(parent);
            ParseRemoveFields(parent);
        }

        private void ParseMatch(XElement parent)
        {
            XElement e = parent.Element("Match");
            Field = e.Attribute("field").Value;
            Match = e.Attribute("value").Value;          
        }

        private void ParseAddFields(XElement parent)
        {
            foreach (var e in parent.Elements("AddField"))
            {                              
                AddFields.Add(new FieldValuePair(ParseStringAttribute(e, "field"), ParseStringAttribute(e, "value")));
            }
        }

        private void ParseDropIfMatch(XElement parent)
        {
            XElement e = parent.Element("DropIfMatch");

            if (e != null)
            {
                string attributeName = "value";
                string value;
                try
                {
                    value = e.Attribute(attributeName).Value;
                }
                catch
                {
                    throw new TimberWinR.ConfigurationErrors.MissingRequiredAttributeException(e, attributeName);
                }


                if (value == "ON" || value == "true")
                {
                    DropIfMatch = true;
                }
                else if (value == "OFF" || value == "false")
                {
                    DropIfMatch = false;
                }
                else
                {
                    throw new TimberWinR.ConfigurationErrors.InvalidAttributeValueException(e.Attribute(attributeName));
                }
            }
        }

        private void ParseRemoveFields(XElement parent)
        {
            foreach (var e in parent.Elements("RemoveField"))
            {
                if (e != null)
                {
                    string attributeName = "value";
                    string value;
                    try
                    {
                        value = e.Attribute(attributeName).Value;
                    }
                    catch
                    {
                        throw new TimberWinR.ConfigurationErrors.MissingRequiredAttributeException(e, attributeName);
                    }
                    RemoveFields.Add(e.Attribute("value").Value);
                }
            }
        }

        /// <summary>
        /// Apply the Grok filter to the Object
        /// </summary>
        /// <param name="json"></param>
        public override void Apply(Newtonsoft.Json.Linq.JObject json)
        {
            if (ApplyMatch(json))
            {
                foreach (var at in AddTags)
                    at.Apply(json);
            }        
        }

        /// <summary>
        /// Apply the Match filter, if there is none specified, it's considered a match.
        /// </summary>
        /// <param name="json"></param>
        /// <returns></returns>
        private bool ApplyMatch(Newtonsoft.Json.Linq.JObject json)
        {            
            JToken token = null;
            if (json.TryGetValue(Field, StringComparison.OrdinalIgnoreCase, out token))
            {
                string text = token.ToString();
                if (!string.IsNullOrEmpty(text))
                {
                    string expr = Match;
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
                return false; // Empty field is no match
            }
            return true; // Not specified is success
        }

        private void AddOrModify(JObject json, string fieldName, string fieldValue)
        {
            if (json[fieldName] == null)
                json.Add(fieldName, fieldValue);
            else
                json[fieldName] = fieldValue;
        }

        public class FieldValuePair
        {
            public string Field { get; set; }
            public string Value { get; set; }

            public FieldValuePair(string field, string value)
            {
                Field = field;
                Value = value;
            }
        }

        private void ParseAddTags(XElement parent)
        {
            foreach (var e in parent.Elements("AddTag"))
            {
                AddTags.Add(new AddTag(e));
            }
        }

        public class AddTag
        {
            public string Value { get; set; }
            public AddTag(XElement e)
            {
                Value = e.Value.Trim();
            }

            public void Apply(Newtonsoft.Json.Linq.JObject json)
            {
                string value = ReplaceTokens(Value, json);
                JToken tags = json["tags"];
                if (tags == null)
                    json.Add("tags", new JArray(value));
                else
                {
                    JArray a = tags as JArray;
                    a.Add(value);
                }
            }

            private string ReplaceTokens(string fieldName, JObject json)
            {
                foreach (var token in json.Children())
                {
                    string replaceString = "%{" + token.Path + "}";
                    fieldName = fieldName.Replace(replaceString, json[token.Path].ToString());
                }
                return fieldName;
            }

        }  
    }

    public struct Pair
    {
        public readonly string Name, Value;

        public Pair(string name, string value)
        {
            Name = name;
            Value = value;
        }

        public override string ToString()
        {
            return String.Format("Name:= {0} , Value:= {1}", Name, Value);
        }
    }

}
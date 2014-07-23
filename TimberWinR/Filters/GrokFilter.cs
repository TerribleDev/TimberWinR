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
        public const string TagName = "Grok";

        public string Match { get; private set; }
        public string Field { get; private set; }
        public List<FieldValuePair> AddFields { get; private set; }
        public bool DropIfMatch { get; private set; }
        public List<string> RemoveFields { get; private set; }

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
            AddFields = new List<FieldValuePair>();
            RemoveFields = new List<string>();

            DropIfMatch = ParseBoolAttribute(parent, "dropIfMatch", false);

            ParseMatch(parent);
            ParseAddFields(parent);
            ParseDropIfMatch(parent);
            ParseRemoveFields(parent);
        }

        private void ParseMatch(XElement parent)
        {
            XElement e = parent.Element("Match");

            Match = e.Attribute("value").Value;
            Field = e.Attribute("field").Value;
        }

        private void ParseAddFields(XElement parent)
        {
            foreach (var e in parent.Elements("AddField"))
            {
                string attributeName = "field";
                string field, value;

                try
                {
                    field = e.Attribute(attributeName).Value;
                }
                catch
                {
                    throw new TimberWinR.ConfigurationErrors.MissingRequiredAttributeException(e, attributeName);
                }

                attributeName = "value";
                try
                {
                    value = e.Attribute(attributeName).Value;
                }
                catch
                {
                    throw new TimberWinR.ConfigurationErrors.MissingRequiredAttributeException(e, attributeName);
                }

                FieldValuePair a = new FieldValuePair(field, value);
                AddFields.Add(a);
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

        public override void Apply(Newtonsoft.Json.Linq.JObject json)
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
                    }
                }
            }
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
using Newtonsoft.Json.Linq;
using RapidRegex.Core;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace TimberWinR.Filters
{
    public class GrokFilter : FilterBase
    {
        public string Match { get; private set; }
        public string Field { get; private set; }
        public Pair AddField { get; private set; }
        public bool DropIfMatch { get; private set; }
        public string RemoveField { get; private set; }

        public GrokFilter(Params_GrokFilter args)
        {
            Match = args.Match;
            Field = args.Field;
            AddField = args.AddField;
            DropIfMatch = args.DropIfMatch;
            RemoveField = args.RemoveField;
        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("GrokFilter\n");
            foreach (var prop in this.GetType().GetProperties())
            {
                if (prop != null)
                {
                    sb.Append(String.Format("\t{0}: {1}\n", prop.Name, prop.GetValue(this, null)));
                }

            }
            return sb.ToString();
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

                            //if (fieldName == "timestamp")
                            //{
                            //    string value = namedCaptures[fieldName];
                            //    DateTime ts;
                            //    if (DateTime.TryParse(value, out ts))
                            //        json.Add(fieldName, ts.ToUniversalTime());
                            //    else if (DateTime.TryParseExact(value, new string[]
                            //    {
                            //        "MMM dd hh:mm:ss",
                            //        "MMM dd HH:mm:ss",
                            //        "MMM dd h:mm",
                            //        "MMM dd hh:mm",
                            //    }, CultureInfo.InvariantCulture, DateTimeStyles.None, out ts))
                            //        json.Add(fieldName, ts.ToUniversalTime());
                            //    else
                            //        json.Add(fieldName, (JToken) namedCaptures[fieldName]);
                            //}
                            //else
                                json.Add(fieldName, (JToken) namedCaptures[fieldName]);
                        }
                    }
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

        public class Params_GrokFilter
        {
            public string Match { get; private set; }
            public string Field { get; private set; }
            public Pair AddField { get; private set; }
            public bool DropIfMatch { get; private set; }
            public string RemoveField { get; private set; }

            public class Builder
            {
                private string match;
                private string field;
                private Pair addField;
                private bool dropIfMatch = false;
                private string removeField;

                public Builder WithField(string value)
                {
                    field = value;
                    return this;
                }

                public Builder WithMatch(string value)
                {
                    match = value;
                    return this;
                }

                public Builder WithAddField(Pair value)
                {
                    addField = value;
                    return this;
                }

                public Builder WithDropIfMatch(bool value)
                {
                    dropIfMatch = value;
                    return this;
                }

                public Builder WithRemoveField(string value)
                {
                    removeField = value;
                    return this;
                }

                public Params_GrokFilter Build()
                {
                    return new Params_GrokFilter()
                    {
                        Match = match,
                        Field = field,
                        AddField = addField,
                        DropIfMatch = dropIfMatch,
                        RemoveField = removeField
                    };
                }

            }
        }
    }
}
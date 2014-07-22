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
        public TimberWinR.Configuration.Pair AddField { get; private set; }
        public bool DropIfMatch { get; private set; }
        public string RemoveField { get; private set; }

        public GrokFilter(TimberWinR.Configuration.Params_Grok args)
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
            sb.Append("Grok\n");
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

    }
}
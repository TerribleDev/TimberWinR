using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Xml;
using Newtonsoft.Json.Linq;
using System.Xml.Linq;
using TimberWinR.Parser;
using RapidRegex.Core;
using System.Text.RegularExpressions;

namespace TimberWinR.Parser
{
    public partial class DateFilter : LogstashFilter
    {                               
        public override bool Apply(JObject json)
        {
            if (Condition != null && !EvaluateCondition(json, Condition))
                return false;

            if (Matches(json))
            {
                ApplyFilter(json);
                AddFields(json);
            }
           
            return true;
        }


        private void ApplyFilter(JObject json)
        {           
            string text = json.ToString();
            if (!string.IsNullOrEmpty(text))
            {
                DateTime ts;
                if (Patterns == null || Patterns.Length == 0)
                {
                    if (DateTime.TryParse(text, out ts))
                        AddOrModify(json, ts);
                }
                else
                {
                    if (DateTime.TryParseExact(text, Patterns.ToArray(), CultureInfo.InvariantCulture,
                        DateTimeStyles.None, out ts))
                        AddOrModify(json, ts);
                }               
            }
        }

        // copy_field "field1" -> "field2"
        private void AddFields(Newtonsoft.Json.Linq.JObject json)
        {
            string srcField = Match[0];

            if (AddField != null && AddField.Length > 0)
            {
                for (int i = 0; i < AddField.Length; i++)
                {
                    string dstField = ExpandField(AddField[i], json);                 
                    if (json[srcField] != null)
                        AddOrModify(json, dstField, json[srcField]);
                }
            }
        }


        private bool Matches(Newtonsoft.Json.Linq.JObject json)
        {
            string field = Match[0];

            CultureInfo ci = new CultureInfo(Locale);

            JToken token = null;
            if (json.TryGetValue(field, out token))
            {
                string text = token.ToString();
                if (!string.IsNullOrEmpty(text))
                {
                    DateTime ts;
                    var exprArray = Match.Skip(1).ToArray();
                     var resolver = new RegexGrokResolver();
                    for (int i=0; i<exprArray.Length; i++)
                    {
                        var pattern = resolver.ResolveToRegex(exprArray[i]);
                        exprArray[i] = pattern;
                    }
                    if (DateTime.TryParseExact(text, exprArray, ci,DateTimeStyles.None, out ts))
                        AddOrModify(json, ts);                    
                }
                return true; // Empty field is no match
            }
            return false; // Not specified is failure
        }


        private void AddOrModify(JObject json, DateTime ts)
        {
            if (ConvertToUTC)
                ts = ts.ToUniversalTime();

            if (json[Target] == null)
                json.Add(Target, ts);
            else
                json[Target] = ts;
        }
    }
}

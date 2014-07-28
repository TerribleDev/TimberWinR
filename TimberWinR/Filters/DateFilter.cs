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
            if (Matches(json))
            {
                ApplyFilter(json);
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

        private bool Matches(Newtonsoft.Json.Linq.JObject json)
        {
            string field = Match[0];            

            JToken token = null;
            if (json.TryGetValue(field, out token))
            {
                string text = token.ToString();
                if (!string.IsNullOrEmpty(text))
                {
                    DateTime ts;
                    var exprArray = Match.Skip(1).ToArray();
                    if (DateTime.TryParseExact(text, exprArray, CultureInfo.InvariantCulture,DateTimeStyles.None, out ts))
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

            //if (json[Target] == null)
            //    json.Add(Target, ts);
            //else
            //    json[Target] = ts;
        }
    }
}

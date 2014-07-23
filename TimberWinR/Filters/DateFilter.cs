using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Xml;
using Newtonsoft.Json.Linq;
using System.Xml.Linq;

namespace TimberWinR.Filters
{
    public class DateFilter : FilterBase
    {
        public new const string TagName = "Date";

        public string Field { get; private set; }
        public string Target { get; private set; }
        public bool ConvertToUTC { get; private set; }
        public List<string> Patterns { get; private set; }

        public static void Parse(List<FilterBase> filters, XElement dateElement)
        {
            filters.Add(parseDate(dateElement));
        }

        static DateFilter parseDate(XElement e)
        {
            return new DateFilter(e);
        }

        DateFilter(XElement parent)
        {
            Patterns = new List<string>();

            Field = ParseStringAttribute(parent, "field");
            Target = ParseStringAttribute(parent, "target", Field);
            ConvertToUTC = ParseBoolAttribute(parent, "convertToUTC", false);                        
            ParsePatterns(parent);
        }

        
        private void ParsePatterns(XElement parent)
        {
            foreach (var e in parent.Elements("Pattern"))
            {
                string pattern = e.Value;
                Patterns.Add(pattern);
            }
        }

        public override void Apply(JObject json)
        {
            JToken token = null;
            if (json.TryGetValue(Field, StringComparison.OrdinalIgnoreCase, out token))
            {
                string text = token.ToString();
                if (!string.IsNullOrEmpty(text))
                {
                    DateTime ts;
                    if (Patterns == null || Patterns.Count == 0)
                    {
                        if (DateTime.TryParse(text, out ts))
                            AddOrModify(json, ts);
                    }
                    else
                    {
                        if (DateTime.TryParseExact(text, Patterns.ToArray(), CultureInfo.InvariantCulture, DateTimeStyles.None, out ts))
                            AddOrModify(json, ts);
                    }
                }
            }
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

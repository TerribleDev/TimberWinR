using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using Newtonsoft.Json.Linq;

namespace TimberWinR.Filters
{
    public class DateFilter : FilterBase
    {
        public string Field { get; private set; }
        public string Target { get; private set; }
        public bool ConvertToUTC { get; private set; }
        public List<string> Patterns { get; private set; }

        public DateFilter()
        {
            Patterns = new List<string>();
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
                        {
                            if (ConvertToUTC)
                                ts = ts.ToUniversalTime();

                            if (json[Target] == null)
                                json.Add(Target, ts);
                            else
                                json[Target] = ts;
                        }
                    }
                    else
                    {
                        if (DateTime.TryParseExact(text, Patterns.ToArray(), CultureInfo.InvariantCulture,
                            DateTimeStyles.None, out ts))
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
            }
        }
    }
}

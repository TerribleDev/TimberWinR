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

        public DateFilter(Params_DateFilter args)
        {
            Field = args.Field;
            Target = args.Target;
            ConvertToUTC = args.ConvertToUTC;
            Patterns = args.Patterns;
        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("DateFilter\n");
            foreach (var prop in this.GetType().GetProperties())
            {
                if (prop != null)
                {
                    sb.Append(String.Format("\t{0}: {1}\n", prop.Name, prop.GetValue(this, null)));
                }

            }
            return sb.ToString();
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

        public class Params_DateFilter
        {
            public string Field { get; private set; }
            public string Target { get; private set; }
            public bool ConvertToUTC { get; private set; }
            public List<string> Patterns { get; private set; }

            public class Builder
            {
                private string field;
                private string target;
                private bool convertToUTC = false;
                private List<string> patterns;

                public Builder WithField(string value)
                {
                    field = value;
                    return this;
                }

                public Builder WithTarget(string value)
                {
                    target = value;
                    return this;
                }

                public Builder WithConvertToUTC(bool value)
                {
                    convertToUTC = value;
                    return this;
                }

                public Builder WithPattern(string value)
                {
                    patterns.Add(value);
                    return this;
                }

                public Params_DateFilter Build()
                {
                    return new Params_DateFilter()
                    {
                        Field = field,
                        Target = target,
                        ConvertToUTC = convertToUTC,
                        Patterns = patterns
                    };
                }
            }
        }
    }
}

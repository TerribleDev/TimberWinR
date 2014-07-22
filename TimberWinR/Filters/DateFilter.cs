using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TimberWinR.Filters
{
    public class DateFilter : FilterBase
    {
        public string Field { get; private set; }
        public string Target { get; private set; }
        public bool ConvertToUTC { get; private set; }
        public List<string> Pattern { get; private set; }

        public DateFilter(Params_DateFilter args)
        {
            Field = args.Field;
            Target = args.Target;
            ConvertToUTC = args.ConvertToUTC;
            Pattern = args.Pattern;
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

        public override void Apply(Newtonsoft.Json.Linq.JObject json)
        {
            throw new NotImplementedException();
        }

        public class Params_DateFilter
        {
            public string Field { get; private set; }
            public string Target { get; private set; }
            public bool ConvertToUTC { get; private set; }
            public List<string> Pattern { get; private set; }

            public class Builder
            {
                private string field;
                private string target;
                private bool convertToUTC = false;
                private List<string> pattern;

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
                    pattern.Add(value);
                    return this;
                }

                public Params_DateFilter Build()
                {
                    return new Params_DateFilter()
                    {
                        Field = field,
                        Target = target,
                        ConvertToUTC = convertToUTC,
                        Pattern = pattern
                    };
                }

            }
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

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
            throw new NotImplementedException();
        }
    }

}

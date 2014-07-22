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

        public override void Apply(Newtonsoft.Json.Linq.JObject json)
        {
            throw new NotImplementedException();
        }
    }
}

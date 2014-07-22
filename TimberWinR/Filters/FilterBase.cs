using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Newtonsoft.Json.Linq;

namespace TimberWinR.Filters
{
    public abstract class FilterBase
    {
        public abstract void Apply(JObject json);       
    }
}

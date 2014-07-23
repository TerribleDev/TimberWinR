using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using Newtonsoft.Json.Linq;

namespace TimberWinR.Filters
{
    public abstract class FilterBase : Parsers
    {
        public const string TagName = "Filters";

        public abstract void Apply(JObject json);

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();

            sb.Append(String.Format("{0}\n", this.GetType().ToString()));
            foreach (var prop in this.GetType().GetProperties())
            {
                if (prop != null)
                {
                    if (prop.PropertyType == typeof(List<>)) 
                    {
                        sb.Append(String.Format("\t{0}: ", prop.Name));
                        foreach (var element in prop.GetValue(this, null) as List<object>)
                        {
                            sb.Append(String.Format("{0},", element));
                        }
                        sb.Append("\n");
                    }
                    sb.Append(String.Format("\t{0}: {1}\n", prop.Name, prop.GetValue(this, null)));
                }

            }
            return sb.ToString();
        }

    }
}

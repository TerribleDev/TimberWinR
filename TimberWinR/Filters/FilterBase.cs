using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using Newtonsoft.Json.Linq;

namespace TimberWinR.Filters
{
    public abstract class FilterBase
    {
        public abstract void Apply(JObject json);

        protected static string ParseStringAttribute(XElement e, string attributeName, string defaultValue="")
        {
            string retValue = defaultValue;
            XAttribute a = e.Attribute(attributeName);
            if (a != null)
                retValue = a.Value;
            return retValue;
        }

        protected static bool ParseBoolAttribute(XElement e, string attributeName, bool defaultValue)
        {
            bool retValue = defaultValue;
            XAttribute a = e.Attribute(attributeName);

            if (a != null)
            {
                switch (a.Value)
                {
                    case "ON":
                    case "true":
                        retValue = true;
                        break;

                    case "OFF":
                    case "false":
                        retValue = false;
                        break;
                }
            }
            return retValue;
        }         
    }
}

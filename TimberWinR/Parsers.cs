using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Xml;
using System.Xml.Linq;
using TimberWinR.Inputs;

public abstract class Parsers
{
    protected static string ParseRequiredStringAttribute(XElement e, string attributeName)
    {
        XAttribute a = e.Attribute(attributeName);
        if (a != null)
            return a.Value;
        else
            throw new TimberWinR.ConfigurationErrors.MissingRequiredAttributeException(e, attributeName);
    }

    protected static string ParseStringAttribute(XElement e, string attributeName, string defaultValue = "")
    {
        string retValue = defaultValue;
        XAttribute a = e.Attribute(attributeName);
        if (a != null)
            retValue = a.Value;
        return retValue;
    }

    protected static string ParseDateAttribute(XElement e, string attributeName, string defaultValue = "")
    {
        string retValue = defaultValue;
        XAttribute a = e.Attribute(attributeName);
        if (a != null)
        {
            DateTime dt;
            if (DateTime.TryParseExact(a.Value,
                "yyyy-MM-dd hh:mm:ss",
                CultureInfo.InvariantCulture,
                DateTimeStyles.None,
                out dt))
            {
                retValue = a.Value;
            }
            else
            {
                throw new TimberWinR.ConfigurationErrors.InvalidAttributeDateValueException(a);
            }
        }

        return retValue;
    }

    protected static bool ParseRequiredBoolAttribute(XElement e, string attributeName)
    {
        XAttribute a = e.Attribute(attributeName);
        if (a == null)
            throw new TimberWinR.ConfigurationErrors.InvalidAttributeValueException(e.Attribute(attributeName));

        switch (a.Value)
        {
            case "ON":
            case "true":
                return true;

            case "OFF":
            case "false":
                return false;

            default:
                throw new TimberWinR.ConfigurationErrors.InvalidAttributeValueException(e.Attribute(attributeName));
        }
    }

    protected static string ParseEnumAttribute(XElement e, string attributeName, IEnumerable<string> values, string defaultValue)
    {
        XAttribute a = e.Attribute(attributeName);

        if (a != null)
        {
            string v = a.Value;
            if (values.Contains(v))
                return v;
            else
                throw new TimberWinR.ConfigurationErrors.InvalidAttributeValueException(e.Attribute(attributeName));
        }
        return defaultValue;
    }

    protected static int ParseIntAttribute(XElement e, string attributeName, int defaultValue)
    {
        XAttribute a = e.Attribute(attributeName);
        if (a != null)
        {
            int valInt;
            if (int.TryParse(a.Value, out valInt))
                return valInt;
            else
                throw new TimberWinR.ConfigurationErrors.InvalidAttributeIntegerValueException(a);
        }
        return defaultValue;
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

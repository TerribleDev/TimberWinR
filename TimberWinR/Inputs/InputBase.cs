using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Xml.Linq;

namespace TimberWinR.Inputs
{
    public abstract class InputBase
    {
        internal List<FieldDefinition> parseFields(XElement parent, Dictionary<string, Type> allPossibleFields)
        {
            IEnumerable<XElement> xml_fields =
                    from el in parent.Elements("Fields").Elements("Field")
                    select el;

            List<FieldDefinition> fields = new List<FieldDefinition>();

            foreach (XElement f in xml_fields)
            {
                // Parse field name.
                string name;
                string attributeName = "name";
                try
                {
                    name = f.Attribute(attributeName).Value;
                }
                catch (NullReferenceException)
                {
                    throw new TimberWinR.ConfigurationErrors.MissingRequiredAttributeException(f, attributeName);
                }

                // Ensure field name is valid.
                if (allPossibleFields.ContainsKey(name))
                {
                    fields.Add(new FieldDefinition(name, allPossibleFields[name]));
                }
                else
                {
                    throw new TimberWinR.ConfigurationErrors.InvalidAttributeValueException(f.Attribute("name"));
                }
            }

            // If no fields are provided, default to all fields.
            if (fields.Count == 0)
            {
                foreach (KeyValuePair<string, Type> entry in allPossibleFields)
                {
                    fields.Add(new FieldDefinition(entry.Key, entry.Value));
                }
            }

            return fields;
        }

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

        protected static bool ParseRequiredBoolAttribute(XElement e, string attributeName)
        {           
            XAttribute a = e.Attribute(attributeName);
            if (a == null)
                throw new TimberWinR.ConfigurationErrors.InvalidAttributeValueException(e.Attribute(attributeName));

            bool retValue = false;
            switch (a.Value)
            {
                case "ON":
                case "true":
                    return true;

                case "OFF":
                case "false":
                    return false;
                    break;

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
}

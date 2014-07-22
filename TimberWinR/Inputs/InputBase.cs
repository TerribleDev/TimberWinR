using System;
using System.Collections.Generic;
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

    }
}

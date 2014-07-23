using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Xml.Linq;

namespace TimberWinR.Inputs
{
    public abstract class InputBase : Parsers
    {
        public const string TagName = "Inputs";

        protected static List<FieldDefinition> ParseFields(XElement parent, Dictionary<string, Type> allPossibleFields)
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

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append(String.Format("{0}\n", this.GetType().ToString()));
            sb.Append("Parameters:\n");
            foreach (var prop in this.GetType().GetProperties())
            {
                if (prop != null)
                {
                    if (prop.Name == "Fields")
                    {
                        sb.Append(String.Format("{0}:\n", prop.Name));
                        foreach (FieldDefinition f in (List<FieldDefinition>)prop.GetValue(this, null))
                        {
                            sb.Append(String.Format("\t{0}\n", f.Name));
                        }
                    }
                    else
                    {
                        sb.Append(String.Format("\t{0}: {1}\n", prop.Name, prop.GetValue(this, null)));
                    }
                }
            }
            return sb.ToString();
        }
    }
}
